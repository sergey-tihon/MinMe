using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;

using DocumentFormat.OpenXml.Packaging;

using Microsoft.IO;

using MinMe.Optimizers.ImageOptimizerRuntime.ImageEngine;
using MinMe.Optimizers.ImageOptimizerRuntime.Model;
using MinMe.Optimizers.ImageOptimizerRuntime.Utils;
namespace MinMe.Optimizers.ImageOptimizerRuntime
{
    internal abstract class OptimizerBase<TDocument>
    {
        protected readonly ImageOptimizerOptions Options;
        private readonly IImageEngine _imageEngineBase;

        protected OptimizerBase(RecyclableMemoryStreamManager manager, ImageOptimizerOptions options)
        {
            Options = options;
            _imageEngineBase = new SystemDrawingEngine(manager);
            //_imageEngineBase = new ChooseBestImageEngine(new ImageSharpEngine(manager), new SystemDrawingEngine(manager));
        }

        public void Transform(TDocument document, CancellationToken token)
        {
            if (Options.RemoveUnusedParts)
            {
                RemoveUnusedPieces(document);
            }

            // Create metadata object for each image inside OpenXmlPackage
            var imagesMetadata = new Dictionary<string, ImageMetadata>();
            foreach (var imagePart in LoadAllImageParts(document))
            {
                var uri = imagePart.Uri.ToString();
                if (imagesMetadata.ContainsKey(uri))
                    continue;
                imagesMetadata.Add(uri, new ImageMetadata(imagePart));
            }

            // Calculate Scale ratio
            var scaleRatio = GetScaleRatio(document);

            // Analyze image usage
            foreach (var usage in GetImageUsageInfo(document))
            {
                var uri = usage.GetImageUri();
                if (uri is null) // No image case
                    continue;

                if (!imagesMetadata.ContainsKey(uri))
                {
                    var msg = $"Found usage of unknown image '{uri}'";
                    continue;
                }

                var meta = imagesMetadata[uri];
                meta.Sizes.Add(usage.GetScaledSizeInPt(scaleRatio));

                var crop = usage.Crop;
                if (crop is {})
                    meta.Crops.Add(crop);
            }

            ResizeImages(imagesMetadata, token);
        }

        /// <summary>
        /// Remove unused pieces from document
        /// </summary>
        /// <param name="document"></param>
        protected abstract void RemoveUnusedPieces(TDocument document);

        /// <summary>
        /// Load URIs of all images inside package
        /// </summary>
        protected abstract IEnumerable<ImagePart> LoadAllImageParts(TDocument document);

        /// <summary>
        /// Calculate ratio of scaling document's Pt to expected screen's Pt
        /// </summary>
        protected abstract double GetScaleRatio(TDocument document);

        /// <summary>
        /// Get image usage info from the document
        /// </summary>
        protected abstract IEnumerable<ImageUsageInfo> GetImageUsageInfo(TDocument document);

        private void ResizeImages(Dictionary<string, ImageMetadata> imagesMetadata, CancellationToken token)
        {
            foreach (var pair in imagesMetadata)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    ResizeImage(pair.Value);
                }
                catch (Exception)
                {
                    //var msg = $"Cannot resize image {pair.Key}";
                }
            }
        }

        private void ResizeImage(ImageMetadata meta)
        {
            ImageCrop? crop = null;
            var crops = meta.Crops.Distinct().ToList();
            if (crops.Count == 1)
            {
                if (crops[0].IsValid())
                {
                    crop = crops.First();
                }
                else
                {
                    var c = crops.First();
                    var msg = $"Unsupported image crop L/T/R/B={c.Left}/{c.Top}/{c.Right}/{c.Bottom}";
                }
            }

            // Get new image size based on usage meta
            var newSize =
                crops.Count > 1
                    ? (Size?) null // We cannot resize if more than 1 crop in identified
                    : meta.Sizes
                        .Aggregate(new Size(), Converters.Expand)
                        .Restrict(Options.ExpectedScreenSize); // New image cannot be large than target screen size


            Stream? newImageStream;
            using (var stream = meta.ImagePart.GetStream(FileMode.Open, FileAccess.Read))
            {
                var sourceFileSize = stream.Length;
                // No sense to optimize small images, it does not add much compression but can corrupt image quality
                if (sourceFileSize <= Options.MinImageSizeForTransformation)
                    return;

                newImageStream = _imageEngineBase.Transform(stream, crop, newSize);

                // It looks strange, but practically it worth to remove the crop, even if result image looks bigger.
                if (newImageStream is null || (newImageStream.Length >= sourceFileSize && crop is null))
                {
                    newImageStream?.Dispose();
                    return;
                }
            }

            // Replace image by smaller one
            using (var stream = meta.ImagePart.GetStream(FileMode.Create, FileAccess.Write))
            {
                newImageStream.Position = 0;
                newImageStream.CopyTo(stream);
                newImageStream.Dispose();
            }

            // Remove crops from markup, because image was cropped
            if (crop is {})
            {
                foreach (var imageCrop in meta.Crops)
                {
                    imageCrop.RemoveCrop();
                }
                meta.IsCropped = true;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;

using DocumentFormat.OpenXml.Packaging;

using Microsoft.IO;

using MinMe.Optimizers.ImageOptimizerRuntime.Model;
using MinMe.Optimizers.ImageOptimizerRuntime.Utils;
namespace MinMe.Optimizers.ImageOptimizerRuntime
{
    internal abstract class OptimizerBase<TDocument>
    {
        private readonly RecyclableMemoryStreamManager _manager;
        protected readonly ImageOptimizerOptions Options;

        protected OptimizerBase(RecyclableMemoryStreamManager manager, ImageOptimizerOptions options)
            => (_manager, Options) = (manager, options);

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
            var srcImage = LoadImage(meta.ImagePart, out var sourceFileSize);

            // No sense to optimize small images, it does not add much compression but can corrupt image quality
            if (sourceFileSize <= Options.MinImageSizeForTransformation)
                return;

            // Don't optimize image metafile
            // Added MemoryBitmap to condition, because only metafiles(by structure, not by extension) are loaded into MemoryBMP
            if (srcImage.RawFormat.Equals(ImageFormat.Emf)
                || srcImage.RawFormat.Equals(ImageFormat.Wmf)
                || srcImage.RawFormat.Equals(ImageFormat.MemoryBmp))
                return;

            var crops = meta.Crops.Distinct().ToList();
            if (crops.Count == 1)
            {
                var crop = crops.First();
                if (crop.IsValid())
                {
                    srcImage = ImageUtils.CropImage(srcImage, crops.First());
                    foreach (var imageCrop in meta.Crops)
                    {
                        imageCrop.RemoveCrop();
                    }
                    meta.IsCropped = true;
                }
                else
                {
                    var msg = $"Unsupported image crop L/T/R/B={crop.Left}/{crop.Top}/{crop.Right}/{crop.Bottom}";
                }
            }

            // Get new image size based on usage meta
            var newSize =
                meta.Sizes
                    .Aggregate(new Size(), Converters.Expand)
                    .Restrict(srcImage.Size) // New images cannot be larger than source one
                    .Restrict(Options.ExpectedScreenSize); // New image cannot be large than target screen size

            // If we do not know image size - keep it as is
            if (newSize.Width == 0 || newSize.Height == 0)
                newSize = srcImage.Size;

            if (crops.Count > 1) // We cannot resize image
                newSize = srcImage.Size;

            var newImage =
                newSize.Width == srcImage.Size.Width &&
                newSize.Height == srcImage.Size.Height
                    ? new Bitmap(srcImage) // Clone image
                    : ImageUtils.ResizeImage(srcImage, newSize);


            using var newImageStream = _manager.GetStream();
            newImage.Save(newImageStream, ImageFormat.Png);

            var isJpegAllowed = ImageUtils.FormatConverter(srcImage, true).Equals(ImageFormat.Jpeg);
            if (isJpegAllowed)
            {
                var jpegCodec = ImageCodecInfo.GetImageEncoders()
                    .FirstOrDefault(t => t.MimeType == "image/jpeg");
                var encoderParams = new EncoderParameters(1)
                    {Param = {[0] = new EncoderParameter(Encoder.Quality, 80L)}};

                using var jpegImageStream = _manager.GetStream();
                newImage.Save(jpegImageStream, jpegCodec, encoderParams);

                if (jpegImageStream.Length < newImageStream.Length)
                {
                    newImageStream.SetLength(0);
                    newImageStream.Position = 0;

                    jpegImageStream.Position = 0;
                    jpegImageStream.CopyTo(newImageStream);
                }
            }

            if (newImageStream.Length < sourceFileSize || meta.IsCropped)
            {
                using var stream = meta.ImagePart.GetStream(FileMode.Create, FileAccess.Write);
                newImageStream.Position = 0;
                newImageStream.CopyTo(stream);
            }
        }

        /// <summary>
        /// Extract image stream from ImagePart and convert to Bitmap
        /// </summary>
        private static Bitmap LoadImage(ImagePart imagePart, out long fileSize)
        {
            using var stream = imagePart.GetStream(FileMode.Open, FileAccess.Read);
            fileSize = stream.Length;
            try
            {
                return new Bitmap(stream);
            }
            catch (Exception exception)
            {
                throw new FormatException("Image corrupted.", exception);
            }
        }
    }
}
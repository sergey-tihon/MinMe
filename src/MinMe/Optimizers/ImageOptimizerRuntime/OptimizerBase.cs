using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using DocumentFormat.OpenXml.Packaging;

using MinMe.Optimizers.ImageOptimizerRuntime.Model;
using MinMe.Optimizers.ImageOptimizerRuntime.Utils;

using NLog;

namespace MinMe.Optimizers.ImageOptimizerRuntime
{
    internal abstract class OptimizerBase<TDocument>
    {
        private readonly Logger _log = LogManager.GetCurrentClassLogger();

        public void Transform(TDocument document, CancellationToken token)
        {
            RemoveUnusedPieces(document);

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
                    _log.Warn($"Found usage of unknown image '{uri}'");
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
                catch (Exception e)
                {
                    _log.Warn(e, $"Cannot resize image {pair.Key}");
                }
            }
        }

        private void ResizeImage(ImageMetadata meta)
        {
            var srcImage = LoadImage(meta.ImagePart, out var sourceFileSize);

            // No sense to optimize small images, it does not add much compression but can corrupt image quality
            if (sourceFileSize <= 5 * 1024)
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
                    _log.Warn($"Unsupported image crop L/T/R/B={crop.Left}/{crop.Top}/{crop.Right}/{crop.Bottom}");
                }
            }

            // Get new image size based on usage meta
            var newSize =
                meta.Sizes
                    .Aggregate(new Size(), Converters.Expand)
                    .Restrict(srcImage.Size) // New images cannot be larger than source one
                    .Restrict(ImageUsageInfo.ExpectedScreenSize); // New image cannot be large than target screen size

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


            var isJpegAllowed = ImageUtils.FormatConverter(srcImage, true).Equals(ImageFormat.Jpeg);
            var jpegFileSize = isJpegAllowed ? CalculateFileSize(newImage, SaveToJpeg) : long.MaxValue;
            var pngFileSize = CalculateFileSize(newImage, SaveToPng);
            var minFileSize = Math.Min(pngFileSize, jpegFileSize);

            // We have to save image back after cropping (because markup was edited)
            if (minFileSize < sourceFileSize || meta.IsCropped)
            {
                if (minFileSize == jpegFileSize)
                    SaveImage(meta, newImage, SaveToJpeg);
                else // save to png if cropped
                    SaveImage(meta, newImage, SaveToPng);
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

        private static void SaveToPng(Bitmap image, Stream stream)
            => image.Save(stream, ImageFormat.Png);

        private static void SaveToJpeg(Bitmap image, Stream stream)
        {
            var codecs = ImageCodecInfo.GetImageEncoders();
            var jpegCodec = codecs.FirstOrDefault(t => t.MimeType == "image/jpeg");

            var qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 80L);
            var encoderParams = new EncoderParameters(1) {Param = {[0] = qualityParam}};

            image.Save(stream, jpegCodec, encoderParams);
        }

        /// <summary>
        /// Calculate image file size after format conversion
        /// </summary>
        private static long CalculateFileSize(Bitmap image, Action<Bitmap, Stream> saveImageFunc)
        {
            using var stream = new MemoryStream();
            saveImageFunc(image, stream);
            stream.Flush();
            return stream.Length;
        }

        /// <summary>
        /// Save image into ImagePart
        /// </summary>
        private static void SaveImage(ImageMetadata meta, Bitmap image, Action<Bitmap, Stream> saveImageFunc)
        {
            using var stream = meta.ImagePart.GetStream(FileMode.Create, FileAccess.Write);
            saveImageFunc(image, stream);
            stream.Flush();
        }
    }
}
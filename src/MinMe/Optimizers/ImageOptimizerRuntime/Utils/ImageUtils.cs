using System;
using System.Drawing;
using System.Drawing.Imaging;

using MinMe.Optimizers.ImageOptimizerRuntime.Model;

namespace MinMe.Optimizers.ImageOptimizerRuntime.Utils
{
    internal static class ImageUtils
    {
        public static Bitmap ResizeImage(Bitmap source, Size newSize)
        {
            try
            {
                var nPercent = Math.Max(
                    (double) newSize.Width / source.Width,
                    (double) newSize.Height / source.Height);

                var destWidth = (int) (source.Width * nPercent + .5);
                var destHeight = (int) (source.Height * nPercent + .5);

                return new Bitmap(source, destWidth, destHeight);
            }
            catch (Exception)
            {
                //var msg = "ResizeImage Exception";
                return new Bitmap(source);
            }
        }

        public static ImageFormat FormatConverter(Bitmap sourceImage, bool manualAlphaCheck)
        {
            if (IsFormatUndefinedOrIndexed(sourceImage.PixelFormat))
                return ImageFormat.Png;

            var pFormat = sourceImage.PixelFormat;
            var isAlpha = pFormat == PixelFormat.Alpha ||
                          pFormat == PixelFormat.PAlpha ||
                          pFormat == PixelFormat.Format16bppArgb1555 ||
                          pFormat == PixelFormat.Format32bppArgb ||
                          pFormat == PixelFormat.Format32bppPArgb ||
                          pFormat == PixelFormat.Format64bppArgb ||
                          pFormat == PixelFormat.Format64bppPArgb;

            if (isAlpha && manualAlphaCheck)
                isAlpha = AlphaCheck(sourceImage);

            return isAlpha ? ImageFormat.Png : ImageFormat.Jpeg;
        }

        private static bool AlphaCheck(Bitmap sourceImage)
        {
            // We do not check transparency on boarders for large images
            var minX = (sourceImage.Size.Width > 2000) ? 3 : 0;
            var minY = (sourceImage.Size.Height > 1000) ? 3 : 0;
            var maxX = sourceImage.Size.Width - minX;
            var maxY = sourceImage.Size.Height - minY;

            for (var i = minX; i < maxX; i++)
                for (var j = minY; j < maxY; j++)
                    if (sourceImage.GetPixel(i, j).A != 255)
                        return true;
            return false;
        }

        private static bool IsFormatUndefinedOrIndexed(PixelFormat format) =>
            format == PixelFormat.Undefined ||
            format == PixelFormat.DontCare ||
            format == PixelFormat.Indexed ||
            format == PixelFormat.Format1bppIndexed ||
            format == PixelFormat.Format4bppIndexed ||
            format == PixelFormat.Format8bppIndexed ||
            format == PixelFormat.Format16bppGrayScale ||
            format == PixelFormat.Format16bppArgb1555;

        public static Bitmap CropImage(Bitmap srcImage, ImageCrop crop)
        {
            var srcRect = crop.GetRectangle(srcImage.Size);

            var cropImage = new Bitmap(srcRect.Width, srcRect.Height);
            using (var gph = Graphics.FromImage(cropImage))
                gph.DrawImage(srcImage, new Rectangle(0, 0, cropImage.Width, cropImage.Height), srcRect, GraphicsUnit.Pixel);

            return cropImage;
        }
    }
}

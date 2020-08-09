using System.Drawing;
using System.IO;

using ImageMagick;

using Microsoft.IO;

using MinMe.Optimizers.ImageOptimizerRuntime.ImageStrategies;
using MinMe.Optimizers.ImageOptimizerRuntime.Model;

namespace MinMe.Tests.Experimental.ImageStrategies
{
    internal class MagickNetStrategy : ImageBaseStrategy
    {
        public MagickNetStrategy(RecyclableMemoryStreamManager streamManager) : base(streamManager)
        {
        }

        public override Stream? Transform(Stream imageStream, ImageCrop? crop, Size? size)
        {
            using var image = new MagickImage(imageStream);

            var srcImageSize = new Size(image.Width, image.Height);
            var newSize = GetFinalSize(srcImageSize, size);

            if (crop is {})
            {
                var rect = crop.GetRectangle(srcImageSize);
                image.Crop(new MagickGeometry(rect.X, rect.Y, rect.Width, rect.Height));
            }
            if (newSize.Width != srcImageSize.Width || newSize.Height != srcImageSize.Height)
            {
                image.Resize((int) newSize.Width, newSize.Height);
            }

            var newImageStream = StreamManager.GetStream();
            image.Write((Stream) newImageStream, MagickFormat.Png);

            var isJpegAllowed = !image.HasAlpha;
            if (isJpegAllowed)
            {
                var jpegImageStream = StreamManager.GetStream();
                image.Write((Stream) jpegImageStream, MagickFormat.Jpeg);
                if (jpegImageStream.Length < newImageStream.Length)
                {
                    newImageStream.Dispose();
                    return jpegImageStream;
                }
                jpegImageStream.Dispose();
            }

            return newImageStream;
        }
    }
}

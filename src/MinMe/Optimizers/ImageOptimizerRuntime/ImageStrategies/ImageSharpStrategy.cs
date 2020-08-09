using System.IO;

using Microsoft.IO;

using MinMe.Optimizers.ImageOptimizerRuntime.Model;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using Size = System.Drawing.Size;

namespace MinMe.Optimizers.ImageOptimizerRuntime.ImageStrategies
{
    public class ImageSharpStrategy : ImageBaseStrategy
    {
        public ImageSharpStrategy(RecyclableMemoryStreamManager streamManager,
                                  PngEncoder? pngEncoder = null, JpegEncoder? jpegEncoder = null)
            : base(streamManager)
        {
            _pngDecoder = pngEncoder ?? new PngEncoder();
            _jpegEncoder = jpegEncoder ?? new JpegEncoder();
        }

        private readonly PngEncoder _pngDecoder;
        private readonly JpegEncoder _jpegEncoder;

        public override Stream? Transform(Stream imageStream, ImageCrop? crop, Size? size)
        {
            using var image = Image.Load<Rgba32>(imageStream);

            var srcImageSize = new Size(image.Width, image.Height);
            var newSize = GetFinalSize(srcImageSize, size);

            image.Mutate(x =>
            {
                if (crop is {})
                {
                    var rect = crop.GetRectangle(srcImageSize);
                    x.Crop(new Rectangle(rect.X, rect.Y, rect.Width, rect.Height));
                }
                if (newSize.Width != srcImageSize.Width || newSize.Height != srcImageSize.Height)
                {
                    x.Resize(newSize.Width, newSize.Height);
                }
            });

            var newImageStream = StreamManager.GetStream();
            image.Save(newImageStream, _pngDecoder);

            var isJpegAllowed = !AlphaCheck(image); // TODO: check for smarter way
            if (isJpegAllowed)
            {
                var jpegImageStream = StreamManager.GetStream();
                image.Save(jpegImageStream, _jpegEncoder);
                if (jpegImageStream.Length < newImageStream.Length)
                {
                    newImageStream.Dispose();
                    return jpegImageStream;
                }
                jpegImageStream.Dispose();
            }

            return newImageStream;
        }

        private static bool AlphaCheck(Image<Rgba32> image)
        {
            // We do not check transparency on boarders for large images
            var minX = (image.Width > 2000) ? 3 : 0;
            var minY = (image.Height > 1000) ? 3 : 0;
            var maxX = image.Width - minX;
            var maxY = image.Height - minY;

            for (var y = minY; y < maxY; y++)
            {
                var pixelRowSpan = image.GetPixelRowSpan(y);
                for (var x = minX; x < maxX; x++)
                    if (pixelRowSpan[x].A != 255)
                        return true;
            }
            return false;
        }
    }
}

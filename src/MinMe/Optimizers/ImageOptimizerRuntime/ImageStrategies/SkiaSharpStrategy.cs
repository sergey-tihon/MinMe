using Microsoft.IO;
using MinMe.Optimizers.ImageOptimizerRuntime.Model;
using SkiaSharp;
using Size = System.Drawing.Size;

namespace MinMe.Optimizers.ImageOptimizerRuntime.ImageStrategies;

/// <summary>
/// Image strategy backed by SkiaSharp: crops, downsizes and re-encodes images as PNG or JPEG (whichever is smaller).
/// </summary>
/// <remarks>
/// Unsupported formats throw <see cref="NotSupportedException"/>; <see cref="ImageOptimizer"/> records the error and leaves that image unchanged.
/// </remarks>
public class SkiaSharpStrategy(
    RecyclableMemoryStreamManager streamManager,
    SKPngEncoderOptions? pngOptions = null,
    SKJpegEncoderOptions? jpegOptions = null
) : ImageBaseStrategy(streamManager)
{
    private static readonly SKSamplingOptions Sampling = new(SKCubicResampler.Mitchell);

    private readonly SKPngEncoderOptions _pngOptions = pngOptions ?? SKPngEncoderOptions.Default;
    private readonly SKJpegEncoderOptions _jpegOptions =
        jpegOptions
        ?? new SKJpegEncoderOptions(75, SKJpegEncoderDownsample.Downsample420, SKJpegEncoderAlphaOption.Ignore);

    public override Stream? Transform(Stream imageStream, ImageCrop? crop, Size? size)
    {
        // SKManagedStream with disposeManagedStream: false — the caller owns imageStream.
        using var codec =
            SKCodec.Create(new SKManagedStream(imageStream, disposeManagedStream: false), out var result)
            ?? throw new NotSupportedException($"Unsupported image format ({result})");

        var alphaType = codec.Info.AlphaType == SKAlphaType.Opaque ? SKAlphaType.Opaque : SKAlphaType.Unpremul;
        var decodeInfo = codec.Info.WithColorType(SKColorType.Rgba8888).WithAlphaType(alphaType);
        using var source =
            SKBitmap.Decode(codec, decodeInfo)
            ?? throw new InvalidDataException($"Unable to decode {codec.EncodedFormat} image");

        var srcImageSize = new Size(source.Width, source.Height);
        var newSize = GetFinalSize(srcImageSize, size);

        using var cropped = Crop(source, crop, srcImageSize);
        var image = cropped ?? source;

        using var resized =
            newSize != srcImageSize
                ? image.Resize(decodeInfo.WithSize(newSize.Width, newSize.Height), Sampling)
                    ?? throw new InvalidOperationException("Unable to resize image")
                : null;
        image = resized ?? image;

        using var pixmap = image.PeekPixels();

        Stream? pngImageStream = null;
        Stream? jpegImageStream = null;
        try
        {
            pngImageStream = StreamManager.GetStream();
            Encode(pngImageStream, w => pixmap.Encode(w, _pngOptions));

            if (HasTransparentPixels(image))
            {
                var transparentPng = pngImageStream;
                pngImageStream = null;
                return transparentPng;
            }

            jpegImageStream = StreamManager.GetStream();
            Encode(jpegImageStream, w => pixmap.Encode(w, _jpegOptions));
            if (jpegImageStream.Length < pngImageStream.Length)
            {
                var smallerJpeg = jpegImageStream;
                jpegImageStream = null;
                return smallerJpeg;
            }

            var pngResult = pngImageStream;
            pngImageStream = null;
            return pngResult;
        }
        finally
        {
            pngImageStream?.Dispose();
            jpegImageStream?.Dispose();
        }
    }

    private static SKBitmap? Crop(SKBitmap source, ImageCrop? crop, Size srcImageSize)
    {
        if (crop is null)
            return null;
        var rect = crop.GetRectangle(srcImageSize);
        var subset = new SKBitmap();
        if (!source.ExtractSubset(subset, SKRectI.Create(rect.X, rect.Y, rect.Width, rect.Height)))
        {
            subset.Dispose();
            throw new InvalidOperationException($"Unable to crop image to {rect}");
        }
        return subset;
    }

    private static void Encode(Stream target, Func<SKWStream, bool> encode)
    {
        using var wstream = new SKManagedWStream(target);
        if (!encode(wstream))
            throw new InvalidOperationException("Unable to encode image");
    }

    private static bool HasTransparentPixels(SKBitmap image)
    {
        if (image.AlphaType == SKAlphaType.Opaque)
            return false;

        // We do not check transparency on borders for large images
        var minX = image.Width > 2000 ? 3 : 0;
        var minY = image.Height > 1000 ? 3 : 0;
        var maxX = image.Width - minX;
        var maxY = image.Height - minY;

        var pixels = image.GetPixelSpan();
        for (var y = minY; y < maxY; y++)
        {
            var row = pixels.Slice(y * image.RowBytes, image.Width * image.BytesPerPixel);
            for (var x = minX; x < maxX; x++)
                if (row[x * 4 + 3] != 255)
                    return true;
        }
        return false;
    }
}

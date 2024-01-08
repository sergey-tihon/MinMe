using System.Drawing;
using DocumentFormat.OpenXml.Packaging;

using Microsoft.IO;

using MinMe.Optimizers.ImageOptimizerRuntime.ImageStrategies;
using MinMe.Optimizers.ImageOptimizerRuntime.Model;
using MinMe.Optimizers.ImageOptimizerRuntime.Utils;

using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace MinMe.Optimizers.ImageOptimizerRuntime;

internal abstract class OptimizerBase<TDocument>
{
    protected readonly ImageOptimizerOptions Options;
    private readonly IImageStrategy _imageStrategy;

    protected OptimizerBase(RecyclableMemoryStreamManager manager, ImageOptimizerOptions options)
    {
        Options = options;
        _imageStrategy = options.ImageStrategy ?? new ImageSharpStrategy(manager, new PngEncoder
        {
            //CompressionLevel = 9
        }, new JpegEncoder
        {
            //Quality = 70,
            //Subsample = JpegSubsample.Ratio420
        });
    }

    public void Transform(TDocument document, OptimizeDiagnostic diagnostic, CancellationToken token)
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
                var message = $"Found usage of unknown image '{uri}'";
                diagnostic.Errors.Add(new OptimizeError(uri, message));
                continue;
            }

            var meta = imagesMetadata[uri];
            meta.Sizes.Add(usage.GetScaledSizeInPt(scaleRatio));

            var crop = usage.Crop;
            if (crop is {})
                meta.Crops.Add(crop);
        }

        ResizeImages(imagesMetadata, diagnostic, token);
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

    private void ResizeImages(Dictionary<string, ImageMetadata> imagesMetadata, OptimizeDiagnostic diagnostic, CancellationToken token)
    {
        imagesMetadata.ExecuteInParallel(pair =>
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (IsIgnoredImagePart(pair.Value.ImagePart))
                    return;
                ResizeImage(pair.Value, diagnostic);
            }
            catch (Exception e)
            {
                var message = $"[{e.GetType().Name}] {e.Message}";
                diagnostic.Errors.Add(new OptimizeError(pair.Key, message));
            }
        }, Options.DegreeOfParallelism);
    }

    private static bool IsIgnoredImagePart(ImagePart imagePart) =>
        imagePart.ContentType switch
        {
            "image/svg+xml" => true,
            "image/x-emf" => true,
            "image/x-wmf" => true,
            _ => false
        };

    private void ResizeImage(ImageMetadata meta, OptimizeDiagnostic diagnostic)
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
                var imgCrop = crops.First();
                var message = $"Unsupported image crop L/T/R/B={imgCrop.Left}/{imgCrop.Top}/{imgCrop.Right}/{imgCrop.Bottom}";
                diagnostic.Errors.Add(new OptimizeError(meta.ImagePart.Uri.OriginalString, message));
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

            newImageStream = _imageStrategy.Transform(stream, crop, newSize);

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
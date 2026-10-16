using System.Drawing;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.IO;
using MinMe.Optimizers.ImageOptimizerRuntime.ImageStrategies;
using MinMe.Optimizers.ImageOptimizerRuntime.Model;
using MinMe.Optimizers.ImageOptimizerRuntime.Utils;

namespace MinMe.Optimizers.ImageOptimizerRuntime;

internal abstract class OptimizerBase<TRootPart>(
    RecyclableMemoryStreamManager manager,
    ImageOptimizerOptions options
)
    where TRootPart : OpenXmlPart
{
    protected readonly ImageOptimizerOptions Options = options;
    private readonly IImageStrategy _imageStrategy = options.ImageStrategy ?? new SkiaSharpStrategy(manager);
    private readonly RecyclableMemoryStreamManager _streamManager = manager;
    private readonly Lock _errorsLock = new();

    public void Transform(TRootPart rootPart, OptimizeDiagnostic diagnostic, CancellationToken token)
    {
        if (Options.RemoveUnusedParts)
            RemoveUnusedPieces(rootPart);

        // Create metadata object for each image inside OpenXmlPackage
        var imagesMetadata = new Dictionary<string, ImageMetadata>();
        foreach (var imagePart in LoadAllImageParts(rootPart))
        {
            var uri = imagePart.Uri.ToString();
            imagesMetadata.TryAdd(uri, new ImageMetadata(imagePart));
        }

        // Calculate Scale ratio
        var scaleRatio = GetScaleRatio(rootPart);

        // Analyze image usage
        foreach (var usage in GetImageUsageInfo(rootPart))
        {
            var uri = usage.GetImageUri();
            if (uri is null) // No image case
                continue;

            if (!imagesMetadata.TryGetValue(uri, out var meta))
            {
                var message = $"Found usage of unknown image '{uri}'";
                diagnostic.Errors.Add(new OptimizeError(uri, message));
                continue;
            }

            meta.Sizes.Add(usage.GetScaledSizeInPt(scaleRatio));

            var crop = usage.Crop;
            if (crop is { })
                meta.Crops.Add(crop);
        }

        ResizeImages(imagesMetadata, diagnostic, token);
    }

    /// <summary>
    /// Remove unused pieces from document
    /// </summary>
    /// <param name="document"></param>
    protected abstract void RemoveUnusedPieces(TRootPart rootPart);

    /// <summary>
    /// Load URIs of all images inside package
    /// </summary>
    protected abstract IEnumerable<ImagePart> LoadAllImageParts(TRootPart rootPart);

    /// <summary>
    /// Calculate ratio of scaling document's Pt to expected screen's Pt
    /// </summary>
    protected abstract double GetScaleRatio(TRootPart rootPart);

    /// <summary>
    /// Get image usage info from the document
    /// </summary>
    protected abstract IEnumerable<ImageUsageInfo> GetImageUsageInfo(TRootPart rootPart);

    private void ResizeImages(
        Dictionary<string, ImageMetadata> imagesMetadata,
        OptimizeDiagnostic diagnostic,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var batchSize = Options.DegreeOfParallelism < 0
            ? Environment.ProcessorCount
            : Options.DegreeOfParallelism;

        // Open XML package streams are not safe to read and write concurrently. Snapshot a bounded batch,
        // transform those independent streams in parallel, then commit results serially.
        foreach (var batch in imagesMetadata.Chunk(batchSize))
        {
            var sources = new List<(string Key, ImageMetadata Metadata, Stream Source)>(batch.Length);
            foreach (var pair in batch)
            {
                if (IsIgnoredImagePart(pair.Value.ImagePart))
                    continue;

                try
                {
                    using var partStream = pair.Value.ImagePart.GetStream(FileMode.Open, FileAccess.Read);
                    if (partStream.Length <= Options.MinImageSizeForTransformation)
                        continue;

                    var source = _streamManager.GetStream();
                    try
                    {
                        partStream.CopyTo(source);
                        source.Position = 0;
                        sources.Add((pair.Key, pair.Value, source));
                    }
                    catch
                    {
                        source.Dispose();
                        throw;
                    }
                }
                catch (Exception e)
                {
                    AddError(diagnostic, pair.Key, e);
                }
            }

            var resizeResults = new (Stream? ImageStream, ImageCrop? Crop)[sources.Count];
            try
            {
                Parallel.ForEach(
                    Enumerable.Range(0, sources.Count),
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Options.DegreeOfParallelism,
                        CancellationToken = token,
                    },
                    index =>
                    {
                        var source = sources[index];
                        try
                        {
                            resizeResults[index] = ResizeImage(source.Metadata, source.Source, diagnostic);
                        }
                        catch (Exception e)
                        {
                            AddError(diagnostic, source.Key, e);
                        }
                    }
                );

                for (var index = 0; index < sources.Count; index++)
                {
                    token.ThrowIfCancellationRequested();
                    var result = resizeResults[index];
                    if (result.ImageStream is not { } imageStream)
                        continue;

                    try
                    {
                        ApplyResize(sources[index].Metadata, imageStream, result.Crop);
                    }
                    catch (Exception e)
                    {
                        AddError(diagnostic, sources[index].Key, e);
                    }
                }
            }
            finally
            {
                foreach (var source in sources)
                    source.Source.Dispose();
                foreach (var result in resizeResults)
                    result.ImageStream?.Dispose();
            }
        }
    }

    private static bool IsIgnoredImagePart(ImagePart imagePart) =>
        imagePart.ContentType switch
        {
            "image/svg+xml" => true,
            "image/x-emf" => true,
            "image/x-wmf" => true,
            _ => false,
        };

    private (Stream? ImageStream, ImageCrop? Crop) ResizeImage(
        ImageMetadata meta,
        Stream sourceStream,
        OptimizeDiagnostic diagnostic
    )
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
                var message =
                    $"Unsupported image crop L/T/R/B={imgCrop.Left}/{imgCrop.Top}/{imgCrop.Right}/{imgCrop.Bottom}";
                lock (_errorsLock)
                    diagnostic.Errors.Add(
                        new OptimizeError(meta.ImagePart.Uri.OriginalString, message)
                    );
            }
        }

        // Get new image size based on usage meta
        var newSize =
            crops.Count > 1
                ? (Size?)null // We cannot resize if more than 1 crop in identified
                : meta
                    .Sizes.Aggregate(new Size(), Converters.Expand)
                    .Restrict(Options.ExpectedScreenSize); // New image cannot be large than target screen size

        var sourceFileSize = sourceStream.Length;
        var newImageStream = _imageStrategy.Transform(sourceStream, crop, newSize);

        // It looks strange, but practically it worth to remove the crop, even if result image looks bigger.
        if (newImageStream is null || (newImageStream.Length >= sourceFileSize && crop is null))
        {
            newImageStream?.Dispose();
            return (null, null);
        }

        return (newImageStream, crop);
    }

    private static void ApplyResize(ImageMetadata meta, Stream imageStream, ImageCrop? crop)
    {
        using (var stream = meta.ImagePart.GetStream(FileMode.Create, FileAccess.Write))
        {
            imageStream.Position = 0;
            imageStream.CopyTo(stream);
        }

        if (crop is not { })
            return;

        foreach (var imageCrop in meta.Crops)
            imageCrop.RemoveCrop();
        meta.IsCropped = true;
    }

    private void AddError(OptimizeDiagnostic diagnostic, string uri, Exception exception)
    {
        var message = $"[{exception.GetType().Name}] {exception.Message}";
        lock (_errorsLock)
            diagnostic.Errors.Add(new OptimizeError(uri, message));
    }
}

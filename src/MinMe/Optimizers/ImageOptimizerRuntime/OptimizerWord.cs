using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Vml;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.IO;
using MinMe.Optimizers.ImageOptimizerRuntime.Model;
using MinMe.Optimizers.ImageOptimizerRuntime.Utils;
using Picture = DocumentFormat.OpenXml.Drawing.Pictures.Picture;

namespace MinMe.Optimizers.ImageOptimizerRuntime;

internal class OptimizerWord(RecyclableMemoryStreamManager manager, ImageOptimizerOptions options)
    : OptimizerBase<MainDocumentPart>(manager, options)
{
    protected override void RemoveUnusedPieces(MainDocumentPart root) { }

    protected override IEnumerable<ImagePart> LoadAllImageParts(MainDocumentPart root) => root.ImageParts;

    protected override double GetScaleRatio(MainDocumentPart root) =>
        (root.RootElement?.Descendants<PageSize>() ?? [])
            .Select(size => size.Width?.Value)
            .OfType<uint>()
            .Select(width => Converters.TwipToPt((int)width))
            .Append(Options.ExpectedScreenSize.Width)
            .Min();

    protected override IEnumerable<ImageUsageInfo> GetImageUsageInfo(MainDocumentPart root)
    {
        if (root.RootElement is not { } rootElement)
            return [];

        // Analyze single images
        var imageUsageInfos = rootElement
            .Descendants<Picture>()
            .Select(picture => new ImageUsageInfo(
                root,
                picture.BlipFill?.Blip,
                picture.ShapeProperties?.Transform2D,
                picture.BlipFill?.SourceRectangle
            ))
            .ToList();

        // Analyze grouped images
        imageUsageInfos.AddRange(
            rootElement.Descendants<Shape>().Select(shape => new ImageUsageInfo(root, shape))
        );

        return imageUsageInfos;
    }
}

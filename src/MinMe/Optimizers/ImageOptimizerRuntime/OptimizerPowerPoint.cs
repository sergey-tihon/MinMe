using System.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.IO;
using MinMe.Optimizers.ImageOptimizerRuntime.Model;
using MinMe.Optimizers.ImageOptimizerRuntime.Utils;

namespace MinMe.Optimizers.ImageOptimizerRuntime;

internal class OptimizerPowerPoint(RecyclableMemoryStreamManager manager, ImageOptimizerOptions options)
    : OptimizerBase<PresentationPart>(manager, options)
{
    private const int DefaultSlideCx = 9_144_000;
    private const int DefaultSlideCy = 6_858_000;

    protected override void RemoveUnusedPieces(PresentationPart presentationPart)
    {
        var usedLayoutUris = presentationPart
            .SlideParts.Select(slide => slide.SlideLayoutPart?.Uri)
            .OfType<Uri>()
            .ToHashSet();

        if (presentationPart.Presentation?.SlideMasterIdList is not { } slideMasterIdList)
            return;

        var slideMasterIds = new Dictionary<string, SlideMasterId>();
        foreach (var slideMasterId in slideMasterIdList.Elements<SlideMasterId>())
        {
            if (slideMasterId.RelationshipId?.Value is { } relationshipId)
                slideMasterIds.Add(relationshipId, slideMasterId);
        }

        foreach (var masterPart in presentationPart.SlideMasterParts.ToList())
        {
            // Skip if at least one layout is used
            if (masterPart.SlideLayoutParts.Any(layout => usedLayoutUris.Contains(layout.Uri)))
                continue;

            var id = presentationPart.GetIdOfPart(masterPart);
            if (slideMasterIds.TryGetValue(id, out var slideMasterId))
                slideMasterIdList.RemoveChild(slideMasterId);
            presentationPart.DeletePart(masterPart);
        }
    }

    protected override IEnumerable<ImagePart> LoadAllImageParts(PresentationPart presentationPart)
    {
        var imageParts = presentationPart.SlideParts.SelectMany(slide => slide.ImageParts).ToList();
        imageParts.AddRange(
            presentationPart.SlideMasterParts.SelectMany(master => master.ImageParts)
        );
        return imageParts;
    }

    protected override double GetScaleRatio(PresentationPart presentationPart)
    {
        var slideSize = GetSlideSize(presentationPart);
        return Math.Min(
            Options.ExpectedScreenSize.Width / Converters.EmuToPt(slideSize.Width),
            Options.ExpectedScreenSize.Height / Converters.EmuToPt(slideSize.Height)
        );
    }

    protected override IEnumerable<ImageUsageInfo> GetImageUsageInfo(
        PresentationPart presentationPart
    )
    {
        var slideSize = GetSlideSize(presentationPart);

        var result = new List<ImageUsageInfo>();
        foreach (var slide in presentationPart.SlideParts)
            result.AddRange(GetImageUsageFromPart(slide, slideSize));
        foreach (var master in presentationPart.SlideMasterParts)
            result.AddRange(GetImageUsageFromPart(master, slideSize));
        return result;
    }

    private static Size GetSlideSize(PresentationPart presentationPart)
    {
        var slideSize = presentationPart.Presentation?.SlideSize;
        return new Size
        {
            Width = slideSize?.Cx?.Value ?? DefaultSlideCx,
            Height = slideSize?.Cy?.Value ?? DefaultSlideCy,
        };
    }

    private static IEnumerable<ImageUsageInfo> GetImageUsageFromPart(OpenXmlPart part, Size slideSize)
    {
        if (part.RootElement is not { } root)
            return [];

        // Analyze single and grouped images
        var imageUsageInfos = root
            .Descendants<Picture>()
            .Select(pict => new ImageUsageInfo(
                part,
                pict.BlipFill?.Blip,
                pict.ShapeProperties?.Transform2D,
                pict.BlipFill?.SourceRectangle
            ))
            .ToList();

        // Analyze background images
        imageUsageInfos.AddRange(
            root.Descendants<CommonSlideData>()
                .Select(commonSlideData => new ImageUsageInfo(part, commonSlideData, slideSize))
        );

        return imageUsageInfos;
    }
}

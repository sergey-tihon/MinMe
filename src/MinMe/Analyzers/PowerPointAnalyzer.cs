using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using MinMe.Analyzers.Model;
using static System.String;
using Drawing = DocumentFormat.OpenXml.Drawing;
using Picture = DocumentFormat.OpenXml.Presentation.Picture;
using Presentation = DocumentFormat.OpenXml.Presentation;

namespace MinMe.Analyzers;

/// <summary>Analyzes PowerPoint parts, image usages, and slide metadata.</summary>
public sealed class PowerPointAnalyzer : IDisposable
{
    public PowerPointAnalyzer(string fileName)
    {
        _fileName = fileName;
        _fileStream = File.Open(fileName, FileMode.Open);
        _document = OpenXmlFactory.OpenPowerPoint(_fileStream, false, true);
        if (_document.PresentationPart is not { } presentationPart)
        {
            _document.Dispose();
            _fileStream.Dispose();
            throw new InvalidDataException("PresentationPart is missing.");
        }
        _presentationPart = presentationPart;
    }

    private readonly string _fileName;
    private readonly FileStream _fileStream;
    private readonly PresentationDocument _document;
    private readonly PresentationPart _presentationPart;

    public void Dispose()
    {
        _document.Dispose();
        _fileStream.Dispose();
    }

    public FileContentInfo Analyze()
    {
        return new FileContentInfo(_fileName, _fileStream.Length)
        {
            Parts = EnumerateAllParts(),
            PartUsages = GetPartUsageData(),
            Slides = GetSlidesData().ToList(),
        };
    }

    private List<PartInfo> EnumerateAllParts()
    {
        var result = new List<PartInfo>();

        var visitedParts = new HashSet<string>();

        foreach (var idPartPair in _document.Parts)
            ProcessPart(idPartPair.OpenXmlPart);

        result.Sort(
            (x, y) => Compare(x.PartType, y.PartType, StringComparison.InvariantCultureIgnoreCase)
        );

        return result;

        void ProcessPart(OpenXmlPart root)
        {
            var key = root.Uri.OriginalString;
            if (!visitedParts.Add(key))
                return;

            result.Add(new PartInfo(key, root.GetType().Name, root.ContentType, root.GetPartSize()));

            foreach (var refPart in root.DataPartReferenceRelationships)
            {
                var dataPart = refPart.DataPart;
                var dataPartKey = dataPart.Uri.OriginalString;
                if (!visitedParts.Add(dataPartKey))
                    continue;

                result.Add(
                    new PartInfo(
                        dataPartKey,
                        dataPart.GetType().Name,
                        dataPart.ContentType,
                        dataPart.GetPartSize()
                    )
                );
            }

            foreach (var idPartPair in root.Parts)
                ProcessPart(idPartPair.OpenXmlPart);
        }
    }

    private Dictionary<string, List<PartUsageInfo>> GetPartUsageData()
    {
        var usages = new Dictionary<string, List<PartUsageInfo>>(
            StringComparer.InvariantCultureIgnoreCase
        );

        var presentation = _presentationPart;

        foreach (
            var slideId in presentation.Presentation?.SlideIdList?.Elements<Presentation.SlideId>() ?? []
        )
        {
            if (GetPart(slideId.RelationshipId) is not SlidePart slide)
                continue;

            ProcessImages(slide);
            ProcessEmbeddedParts(slide);

            AddUsage(slide.Uri, new Reference(presentation.Uri));
            if (slide.SlideLayoutPart is not { } layout)
                continue;
            AddUsage(layout.Uri, new Reference(slide.Uri));
            if (layout.SlideMasterPart is not { } master)
                continue;
            AddUsage(master.Uri, new Reference(layout.Uri));
            if (master.ThemePart is not { } theme)
                continue;
            AddUsage(theme.Uri, new Reference(master.Uri));
        }

        return usages;

        void AddUsage(Uri uri, PartUsageInfo usage)
        {
            var key = uri.OriginalString;
            if (usages.TryGetValue(key, out var list))
                list.Add(usage);
            else
                usages.Add(key, [usage]);
        }

        OpenXmlPart? GetPart(StringValue? relId) =>
            relId?.Value is { } id ? presentation.GetPartById(id) : null;

        void ProcessImages(OpenXmlPart slide)
        {
            if (slide.RootElement is not { } root)
                return;

            // Analyze single and grouped images
            foreach (var pic in root.Descendants<Picture>())
            {
                var relId = pic.BlipFill?.Blip?.Embed?.Value;
                if (relId is null)
                    continue;
                var uri = slide.GetPartById(relId).Uri;
                var usage = ImageUsageInfo.FromPict(pic);
                AddUsage(uri, new ImageUsage(usage, slide.Uri));
            }

            // Analyze background images
            foreach (var commonSlideData in root.Descendants<Presentation.CommonSlideData>())
            {
                var blipFill = commonSlideData
                    .Background?.BackgroundProperties?.Descendants<Presentation.BlipFill>()
                    .FirstOrDefault();
                var srcRec = blipFill?.SourceRectangle;
                var relId = blipFill?.Blip?.Embed?.Value;
                if (relId is null || srcRec is null)
                    continue;

                var uri = slide.GetPartById(relId).Uri;
                var usage = new ImageUsageInfo(-1, -1, ImageCrop.FromSourceRect(srcRec));
                AddUsage(uri, new ImageUsage(usage, slide.Uri));
            }
        }

        void ProcessEmbeddedParts(SlidePart slide)
        {
            foreach (var part in slide.EmbeddedPackageParts)
                AddUsage(part.Uri, new Reference(slide.Uri));
        }
    }

    private IEnumerable<SlideInfo> GetSlidesData()
    {
        var position = 1;
        foreach (
            var slideId in _presentationPart.Presentation?.SlideIdList?.Elements<Presentation.SlideId>() ?? []
        )
        {
            var number = position++;
            if (
                slideId.RelationshipId?.Value is not { } relationshipId
                || _presentationPart.GetPartById(relationshipId) is not SlidePart slidePart
            )
                continue;

            var title = GetSlideTitle(slidePart);
            yield return new SlideInfo(number, slidePart.Uri.OriginalString, title);
        }
    }

    // Code from Clippit
    private static string GetSlideTitle(SlidePart slidePart)
    {
        var titleShapes = (slidePart.Slide?.CommonSlideData?.ShapeTree?.Descendants<Presentation.Shape>() ?? [])
            .Where(shape =>
            {
                var value = shape
                    .NonVisualShapeProperties
                    ?.ApplicationNonVisualDrawingProperties
                    ?.PlaceholderShape
                    ?.Type
                    ?.Value;

                return value == Presentation.PlaceholderValues.Title
                    || value == Presentation.PlaceholderValues.CenteredTitle;
            })
            .ToList();

        var paragraphText = new StringBuilder();
        foreach (var shape in titleShapes)
        {
            // Get the text in each paragraph in this shape.
            foreach (var paragraph in shape.TextBody?.Descendants<Drawing.Paragraph>() ?? [])
            {
                foreach (var text in paragraph.Descendants<Drawing.Text>())
                    paragraphText.Append(text.Text);
            }
        }

        return paragraphText.ToString().Trim();
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;

using MinMe.Analyzers.Model;

using Presentation = DocumentFormat.OpenXml.Presentation;
using Drawing = DocumentFormat.OpenXml.Drawing;

using static System.String;

using Picture = DocumentFormat.OpenXml.Drawing.Picture;

namespace MinMe.Analyzers
{
    public class PowerPointAnalyzer : IDisposable
    {
        public PowerPointAnalyzer(string fileName)
        {
            _fileName = fileName;
            _fileStream = File.Open(fileName, FileMode.Open);

            var openSettings = new OpenSettings {AutoSave = false};
            try
            {
                _document = PresentationDocument.Open(_fileStream, false, openSettings);
            }
            catch (OpenXmlPackageException e)
            {
                if (!e.ToString().Contains("Invalid Hyperlink"))
                    throw;

                OpenXmlRecovery.FixInvalidUri(_fileStream);
                _document =  PresentationDocument.Open(_fileStream, true, openSettings);
            }
        }

        private readonly string _fileName;
        private readonly FileStream _fileStream;
        private readonly PresentationDocument _document;

        public void Dispose()
        {
            _document?.Dispose();
            _fileStream?.Dispose();
        }

        public FileContentInfo Analyze()
            => new FileContentInfo(_fileName, _fileStream.Length)
            {
                Parts = EnumerateAllParts(),
                PartUsages = GetPartUsageData(),
                Slides = GetSlidesData().ToList()
            };

        private List<PartInfo> EnumerateAllParts()
        {
            var result = new List<PartInfo>();

            var visitedParts = new HashSet<string>();
            void ProcessPart(OpenXmlPart root)
            {
                var key = root.Uri.OriginalString;
                if (visitedParts.Contains(key))
                    return;

                visitedParts.Add(key);
                result.Add(new PartInfo(
                    key, root.GetType().Name,
                    root.ContentType, root.GetPartSize()));

                foreach (var refPart in root.DataPartReferenceRelationships)
                {
                    var dataPart = refPart.DataPart;
                    var dataPartKey = dataPart.Uri.OriginalString;
                    if (visitedParts.Contains(dataPartKey))
                        continue;

                    visitedParts.Add(dataPartKey);
                    result.Add(new PartInfo(
                        dataPartKey, dataPart.GetType().Name,
                        dataPart.ContentType, dataPart.GetPartSize()));
                }

                foreach (var idPartPair in root.Parts)
                    ProcessPart(idPartPair.OpenXmlPart);
            }

            foreach (var idPartPair in _document.Parts)
                ProcessPart(idPartPair.OpenXmlPart);

            result.Sort((x,y) =>
                Compare(x.PartType, y.PartType, StringComparison.InvariantCultureIgnoreCase));
            return result;
        }

        private Dictionary<string, List<PartUsageInfo>> GetPartUsageData()
        {
            var usages = new Dictionary<string, List<PartUsageInfo>>();
            void AddUsage(Uri uri, PartUsageInfo usage)
            {
                var key = uri.OriginalString;
                if (usages.TryGetValue(key, out var list))
                    list.Add(usage);
                else
                    usages.Add(key, new List<PartUsageInfo> {usage});
            }

            var presentation = _document.PresentationPart;
            OpenXmlPart? GetPart(StringValue relId)
                => relId?.HasValue == true ? presentation.GetPartById(relId.Value) : null;

            void ProcessImages(SlidePart slide)
            {
                // Analyze single and grouped images
                foreach (var pic in slide.RootElement.Descendants<Picture>())
                {
                    var relId = pic.BlipFill?.Blip.Embed?.Value;
                    if (relId is null)
                        continue;
                    var uri = slide.GetPartById(relId).Uri;
                    var usage = ImageUsageInfo.FromPict(pic);
                    AddUsage(uri, new ImageUsage(usage));
                }
                // Analyze background images
                foreach (var commonSlideData in slide.RootElement.Descendants<Presentation.CommonSlideData>())
                {
                    var blipFill = commonSlideData?.Background?.BackgroundProperties
                        ?.Descendants<Presentation.BlipFill>()?.FirstOrDefault();
                    var srcRec = blipFill?.SourceRectangle;
                    var relId = blipFill?.Blip?.Embed?.Value;
                    if (relId is null || srcRec is null)
                        continue;

                    var uri = slide.GetPartById(relId).Uri;
                    var usage = new ImageUsageInfo(-1, -1, ImageCrop.FromSourceRect(srcRec));
                    AddUsage(uri, new ImageUsage(usage));
                }
            }

            foreach (var slideId in presentation.Presentation.SlideIdList.ChildElements.OfType<Presentation.SlideId>())
            {
                var slide = GetPart(slideId.RelationshipId) as SlidePart;
                if (slide is null)
                    continue;
                AddUsage(slide.Uri, new Reference(presentation.Uri));
                var layout = slide.SlideLayoutPart;
                AddUsage(layout.Uri, new Reference(slide.Uri));
                var master = layout.SlideMasterPart;
                AddUsage(master.Uri, new Reference(layout.Uri));

                ProcessImages(slide);
            }

            return usages;
        }

        private IEnumerable<SlideInfo> GetSlidesData()
        {
            var presentationPart = _document.PresentationPart;
            var slideIdList = presentationPart.Presentation.SlideIdList;

            var number = 1;
            foreach (var slideId in slideIdList.Cast<Presentation.SlideId>())
            {
                var part = presentationPart.GetPartById(slideId.RelationshipId);
                var title = GetSlideTitle((SlidePart) part);
                yield return new SlideInfo(number++, part.Uri.OriginalString, title);
            }
        }

        // Code from Clippit
        private static string GetSlideTitle(SlidePart slidePart)
        {
            var titleShapes =
                slidePart.Slide.CommonSlideData.ShapeTree
                    .Descendants<Presentation.Shape>()
                    .Where(shape =>
                        shape.NonVisualShapeProperties
                                ?.ApplicationNonVisualDrawingProperties
                                ?.PlaceholderShape?.Type?.Value switch
                            {
                                Presentation.PlaceholderValues.Title => true,
                                Presentation.PlaceholderValues.CenteredTitle => true,
                                _ => false
                            })
                    .ToList();

            var paragraphText = new StringBuilder();
            foreach (var shape in titleShapes)
            {
                // Get the text in each paragraph in this shape.
                foreach (var paragraph in shape.TextBody.Descendants<Drawing.Paragraph>())
                {
                    foreach (var text in paragraph.Descendants<Drawing.Text>())
                    {
                        paragraphText.Append(text.Text);
                    }
                    //if (paragraphText.Length > 0)
                    //    paragraphText.Append(Environment.NewLine);
                }
            }

            return paragraphText.ToString().Trim();
        }

        public Stream? GetThumbnail()
        {
            if (_document.ThumbnailPart is null)
                return null;

            var ms = new MemoryStream();
            using var stream = _document.ThumbnailPart.GetStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            return ms;
        }
    }
}
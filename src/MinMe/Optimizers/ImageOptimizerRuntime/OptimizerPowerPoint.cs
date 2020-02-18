using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

using Microsoft.IO;

using MinMe.Optimizers.ImageOptimizerRuntime.Model;
using MinMe.Optimizers.ImageOptimizerRuntime.Utils;

namespace MinMe.Optimizers.ImageOptimizerRuntime
{
    internal class OptimizerPowerPoint : OptimizerBase<PresentationDocument>
    {
        public OptimizerPowerPoint(RecyclableMemoryStreamManager manager) : base(manager)
        {
        }

        protected override void RemoveUnusedPieces(PresentationDocument document)
        {
            var presentationPart = document.PresentationPart;
            var usedLayoutUris = new HashSet<Uri>(
                presentationPart.SlideParts
                    .Select(slide => slide.SlideLayoutPart.Uri));

            var slideMasterIdList = presentationPart.Presentation.SlideMasterIdList;
            var slideMasterIds =
                slideMasterIdList.ChildElements.Cast<SlideMasterId>()
                    .ToDictionary(x => x.RelationshipId.Value);

            foreach (var masterPart in presentationPart.SlideMasterParts.ToList())
            {
                // Skip if at least one layout is used
                if (masterPart.SlideLayoutParts
                        .Any(layout => usedLayoutUris.Contains(layout.Uri)))
                    continue;

                var id = presentationPart.GetIdOfPart(masterPart);
                slideMasterIdList.RemoveChild(slideMasterIds[id]);
                presentationPart.DeletePart(masterPart);
            }
        }

        protected override IEnumerable<ImagePart> LoadAllImageParts(PresentationDocument document)
        {
            var imageParts =
                document.PresentationPart.SlideParts
                    .SelectMany(slide => slide.ImageParts).ToList();
            imageParts.AddRange(
                document.PresentationPart.SlideMasterParts
                    .SelectMany(master => master.ImageParts));
            return imageParts;
        }

        protected override double GetScaleRatio(PresentationDocument document)
        {
            var slideSize =
                document.PresentationPart.RootElement
                    .GetFirstChild<SlideSize>();
            return
                Math.Min(
                    ImageUsageInfo.ExpectedScreenSize.Width
                        / Converters.EmuToPt(slideSize.Cx.Value),
                    ImageUsageInfo.ExpectedScreenSize.Height
                        / Converters.EmuToPt(slideSize.Cy.Value));
        }

        protected override IEnumerable<ImageUsageInfo> GetImageUsageInfo(PresentationDocument document)
        {
            var presentationPart = document.PresentationPart;
            var slideSizeElement = presentationPart.RootElement.GetFirstChild<SlideSize>();
            var slideSize = new Size { Width = slideSizeElement.Cx.Value, Height = slideSizeElement.Cy.Value };

            var result = new List<ImageUsageInfo>();
            foreach (var slide in presentationPart.SlideParts)
                result.AddRange(
                    GetImageUsageFromPart(slide, slideSize));
            foreach (var master in presentationPart.SlideMasterParts)
                result.AddRange(
                    GetImageUsageFromPart(master, slideSize));
            return result;
        }

        private IEnumerable<ImageUsageInfo> GetImageUsageFromPart(OpenXmlPart part, Size slideSize)
        {
            // Analyze single and grouped images
            var imageUsageInfos =
                part.RootElement.Descendants<Picture>()
                    .Select(pict =>
                        new ImageUsageInfo(
                            part,
                            pict?.BlipFill?.Blip,
                            pict?.ShapeProperties?.Transform2D,
                            pict?.BlipFill?.SourceRectangle))
                    .ToList();

            // Analyze background images
            imageUsageInfos.AddRange(
                part.RootElement.Descendants<CommonSlideData>()
                    .Select(commonSlideData =>
                        new ImageUsageInfo(part, commonSlideData, slideSize)));

            return imageUsageInfos;
        }
    }
}

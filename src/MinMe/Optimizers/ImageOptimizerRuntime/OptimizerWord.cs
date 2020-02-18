using System.Collections.Generic;
using System.Linq;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Vml;
using DocumentFormat.OpenXml.Wordprocessing;

using MinMe.Optimizers.ImageOptimizerRuntime.Model;
using MinMe.Optimizers.ImageOptimizerRuntime.Utils;

using Picture = DocumentFormat.OpenXml.Drawing.Pictures.Picture;

namespace MinMe.Optimizers.ImageOptimizerRuntime
{
    internal class OptimizerWord: OptimizerBase<WordprocessingDocument>
    {
        protected override void RemoveUnusedPieces(WordprocessingDocument document)
        {
        }

        protected override IEnumerable<ImagePart> LoadAllImageParts(WordprocessingDocument document)
            => document.MainDocumentPart.ImageParts;

        protected override double GetScaleRatio(WordprocessingDocument document)
        {
            var width =
                document.MainDocumentPart
                    .RootElement.Descendants<PageSize>()
                    .Select(size => Converters.TwipToPt((int) size.Width.Value))
                    .Concat(new double[] {ImageUsageInfo.ExpectedScreenSize.Width})
                    .Min();

            return ImageUsageInfo.ExpectedScreenSize.Width / width;
        }

        protected override IEnumerable<ImageUsageInfo> GetImageUsageInfo(WordprocessingDocument document)
        {
            var part = document.MainDocumentPart;

            // Analyze single images
            var imageUsageInfos =
                part.RootElement.Descendants<Picture>()
                    .Select(pict =>
                        new ImageUsageInfo(
                            part,
                            pict?.BlipFill?.Blip,
                            pict?.ShapeProperties?.Transform2D,
                            pict?.BlipFill?.SourceRectangle))
                    .ToList();

            // Analyze grouped images
            imageUsageInfos.AddRange(
                part.RootElement.Descendants<Shape>()
                    .Select(shape => new ImageUsageInfo(part, shape)));

            return imageUsageInfos;
        }
    }
}

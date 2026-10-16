using System.Drawing;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Vml;
using DocumentFormat.OpenXml.Wordprocessing;
using MinMe.Optimizers;
using MinMe.Optimizers.ImageOptimizerRuntime.ImageStrategies;
using MinMe.Optimizers.ImageOptimizerRuntime.Model;
using TUnit.Assertions;
using TUnit.Core;

namespace MinMe.Tests.RepoTests;

public class OptimizerWordTests
{
    [Test]
    public async Task WordImageSizeUsesScreenToPageRatio()
    {
        using var source = new MemoryStream();
        using (var document = WordprocessingDocument.Create(source, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            var imagePart = mainPart.AddImagePart(ImagePartType.Png);
            using (var imageBytes = new MemoryStream(new byte[6000]))
                imagePart.FeedData(imageBytes);

            var shape = new Shape { Style = "width:100pt;height:50pt" };
            shape.Append(new ImageData { RelationshipId = mainPart.GetIdOfPart(imagePart) });
            mainPart.Document = new Document(
                new Body(
                    new Paragraph(new Run(new Picture(shape))),
                    new SectionProperties(new PageSize { Width = 12240U, Height = 15840U })
                )
            );
            mainPart.Document.Save();
        }

        source.Position = 0;
        var strategy = new CapturingImageStrategy();
        using var output = new ImageOptimizer().Transform(
            ".docx",
            source,
            out _,
            new ImageOptimizerOptions { ImageStrategy = strategy }
        );

        await Assert.That(strategy.Size).IsEqualTo(new Size(314, 157));
    }

    private sealed class CapturingImageStrategy : IImageStrategy
    {
        public Size? Size { get; private set; }

        public Stream? Transform(Stream imageStream, ImageCrop? crop, Size? size)
        {
            Size = size;
            return null;
        }
    }
}

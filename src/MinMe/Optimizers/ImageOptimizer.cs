using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

using DocumentFormat.OpenXml.Packaging;
using Microsoft.IO;

using MinMe.Optimizers.ImageOptimizerRuntime;


namespace MinMe.Optimizers
{
    public class ImageOptimizer
    {
        private readonly RecyclableMemoryStreamManager _manager = new RecyclableMemoryStreamManager();

        public Stream Transform(string fileType, Stream stream, ImageOptimizerOptions? options = null, CancellationToken? token = null)
        {
            options ??= new ImageOptimizerOptions();
            var cancellationToken = token ?? CancellationToken.None;

            // Copy of the original stream that will be modified in-place
            using var memoryStream = _manager.GetStream();
            stream.CopyTo(memoryStream);

            switch (fileType.ToLower())
            {
                case ".pptx":
                    memoryStream.Position = 0;
                    TransformPptxStream(memoryStream, options, cancellationToken);
                    break;
                case ".docx":
                    memoryStream.Position = 0;
                    TransformDocxStream(memoryStream, options, cancellationToken);
                    break;
                default:
                    var msg = $"ImageOptimizer cannot process {fileType}.";
                    break;
            }

            memoryStream.Position = 0;
            try
            {
                return ReCompress(memoryStream, cancellationToken);
            }
            catch
            {
                return memoryStream; // not Zip archive?
            }
        }

        private void TransformDocxStream(Stream stream, ImageOptimizerOptions options, CancellationToken token)
        {
            using var document = OpenXmlFactory.OpenWord(stream, true, options.OpenXmlUriAutoRecovery);
            var transformation = new OptimizerWord(_manager, options);
            transformation.Transform(document, token);

            foreach (var part in document.MainDocumentPart.EmbeddedPackageParts)
            {
                token.ThrowIfCancellationRequested();
                TransformEmbeddedPart(part, options, token);
            }
        }

        private void TransformPptxStream(Stream stream, ImageOptimizerOptions options, CancellationToken token)
        {
            using var document = OpenXmlFactory.OpenPowerPoint(stream, true, options.OpenXmlUriAutoRecovery);
            var transformation = new OptimizerPowerPoint(_manager, options);
            transformation.Transform(document, token);

            foreach (var slide in document.PresentationPart.SlideParts)
            foreach (var part in slide.EmbeddedPackageParts)
            {
                token.ThrowIfCancellationRequested();
                TransformEmbeddedPart(part, options, token);
            }
        }

        private void TransformEmbeddedPart(EmbeddedPackagePart part, ImageOptimizerOptions options, CancellationToken token)
        {
            // Read mode about office mime types: http://filext.com/faq/office_mime_types.php
            var fileType = part.ContentType
                switch
                {
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                    "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ".pptx",
                    _ => null
                };

            if (fileType is null)
            {
                var msg = $"Unsupported embedding type {part.ContentType}";
                return;
            }

            using var result = Transform(fileType, part.GetStream(), options, token);
            result.Position = 0;
            part.FeedData(result);
        }

        private Stream ReCompress(Stream stream, CancellationToken token)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

            var packedStream = _manager.GetStream();
            using (var newZip = new ZipArchive(packedStream, ZipArchiveMode.Create, true))
            {
                foreach (var entry in zip.Entries)
                {
                    token.ThrowIfCancellationRequested();

                    var newEntry = newZip.CreateEntry(entry.FullName, CompressionLevel.Optimal);

                    using var srcStream = entry.Open();
                    using var dstStream = newEntry.Open();
                    srcStream.CopyTo(dstStream);
                }
            }
            packedStream.Position = 0;
            return packedStream;
        }
    }
}

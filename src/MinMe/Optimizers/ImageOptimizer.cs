using System.IO.Compression;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.IO;

using MinMe.Optimizers.ImageOptimizerRuntime;


namespace MinMe.Optimizers
{
    public class ImageOptimizer
    {
        public ImageOptimizer(RecyclableMemoryStreamManager? memoryStreamManager = null) =>
            _memoryStreamManager = memoryStreamManager ?? new RecyclableMemoryStreamManager();

        private readonly RecyclableMemoryStreamManager _memoryStreamManager;

        public Stream Transform(string fileType, Stream stream, out OptimizeDiagnostic diagnostic, ImageOptimizerOptions? options = null, CancellationToken? token = null)
        {
            options ??= new ImageOptimizerOptions();
            diagnostic = new OptimizeDiagnostic();
            var cancellationToken = token ?? CancellationToken.None;

            // Copy of the original stream that will be modified in-place
            var memoryStream = _memoryStreamManager.GetStream();
            stream.CopyTo(memoryStream);

            switch (fileType.ToLower())
            {
                case ".pptx":
                    memoryStream.Position = 0;
                    TransformPptxStream(memoryStream, diagnostic, options, cancellationToken);
                    break;
                case ".docx":
                    memoryStream.Position = 0;
                    TransformDocxStream(memoryStream, diagnostic, options, cancellationToken);
                    break;
                default:
                    var message = $"ImageOptimizer cannot process {fileType}.";
                    diagnostic.Errors.Add(new OptimizeError("/", message));
                    break;
            }

            memoryStream.Position = 0;
            try
            {
                return options.ReZipAfterOptimization
                    ? ReZip(memoryStream, cancellationToken)
                    : memoryStream;
            }
            catch
            {
                return memoryStream; // not Zip archive?
            }
        }

        private void TransformDocxStream(Stream stream, OptimizeDiagnostic diagnostic, ImageOptimizerOptions options, CancellationToken token)
        {
            using var document = OpenXmlFactory.OpenWord(stream, true, options.OpenXmlUriAutoRecovery);
            var transformation = new OptimizerWord(_memoryStreamManager, options);
            transformation.Transform(document, diagnostic, token);

            foreach (var part in document.MainDocumentPart.EmbeddedPackageParts)
            {
                token.ThrowIfCancellationRequested();
                TransformEmbeddedPart(part, diagnostic, options, token);
            }
        }

        private void TransformPptxStream(Stream stream, OptimizeDiagnostic diagnostic, ImageOptimizerOptions options, CancellationToken token)
        {
            using var document = OpenXmlFactory.OpenPowerPoint(stream, true, options.OpenXmlUriAutoRecovery);
            var transformation = new OptimizerPowerPoint(_memoryStreamManager, options);
            transformation.Transform(document, diagnostic, token);

            foreach (var slide in document.PresentationPart.SlideParts)
            foreach (var part in slide.EmbeddedPackageParts)
            {
                token.ThrowIfCancellationRequested();
                TransformEmbeddedPart(part, diagnostic, options, token);
            }
        }

        private void TransformEmbeddedPart(EmbeddedPackagePart part, OptimizeDiagnostic diagnostic, ImageOptimizerOptions options, CancellationToken token)
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
                var message = $"Unsupported embedding type {part.ContentType}";
                diagnostic.Errors.Add(new OptimizeError(part.Uri.OriginalString, message));
                return;
            }

            using var result = Transform(fileType, part.GetStream(), out var partDiagnostic, options, token);
            result.Position = 0;
            part.FeedData(result);

            foreach (var error in partDiagnostic.Errors)
            {
                var pointer = part.Uri.OriginalString + '|' + error.Pointer;
                diagnostic.Errors.Add(new OptimizeError(pointer, error.Message));
            }
        }

        private Stream ReZip(Stream stream, CancellationToken token)
        {
            using (stream)
            {
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read, false);

                var packedStream = _memoryStreamManager.GetStream();
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
}

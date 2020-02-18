using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

using DocumentFormat.OpenXml.Packaging;

using Microsoft.IO;

using MinMe.Optimizers.ImageOptimizerRuntime;

using NLog;

namespace MinMe.Optimizers
{
    public class ImageOptimizer
    {
        private static readonly RecyclableMemoryStreamManager Manager = new RecyclableMemoryStreamManager();
        private readonly Logger _log = LogManager.GetCurrentClassLogger();

        public Stream Transform(string fileType, Stream stream, CancellationToken? token = null)
        {
            var cancellationToken = token ?? CancellationToken.None;

            // Copy of the original stream that will be modified in-place
            using var memoryStream = Manager.GetStream();
            stream.CopyTo(memoryStream);

            switch (fileType.ToLower())
            {
                case ".pptx":
                    memoryStream.Position = 0;
                    TransformPptxStream(memoryStream, cancellationToken);
                    break;
                case ".docx":
                    memoryStream.Position = 0;
                    TransformDocxStream(memoryStream, cancellationToken);
                    break;
                default:
                    _log.Warn($"ImageOptimizer cannnot process {fileType}.");
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

        private void TransformDocxStream(Stream stream, CancellationToken token)
        {
            using var document = WordprocessingDocument.Open(stream, true);
            var transformation = new OptimizerWord(Manager);
            transformation.Transform(document, token);

            foreach (var part in document.MainDocumentPart.EmbeddedPackageParts)
            {
                token.ThrowIfCancellationRequested();
                TransformEmbeddedPart(part, token);
            }
        }

        private void TransformPptxStream(Stream stream, CancellationToken token)
        {
            using var document = PresentationDocument.Open(stream, true);
            var transformation = new OptimizerPowerPoint(Manager);
            transformation.Transform(document, token);

            foreach (var slide in document.PresentationPart.SlideParts)
            foreach (var part in slide.EmbeddedPackageParts)
            {
                token.ThrowIfCancellationRequested();
                TransformEmbeddedPart(part, token);
            }
        }

        private void TransformEmbeddedPart(EmbeddedPackagePart part, CancellationToken token)
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
                _log.Info($"Unsupported embedding type {part.ContentType}");
                return;
            }

            using var result = Transform(fileType, part.GetStream(), token);
            result.Position = 0;
            part.FeedData(result);
        }

        private Stream ReCompress(Stream stream, CancellationToken token)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

            var packedStream = Manager.GetStream();
            using (var newZip = new ZipArchive(packedStream, ZipArchiveMode.Create, true))
            {
                try
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
                catch (OperationCanceledException cancellationException)
                {
                    _log.Warn(cancellationException, cancellationException.Message);
                    throw;
                }
            }
            packedStream.Position = 0;
            return packedStream;
        }
    }
}
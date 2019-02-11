module MinMe.OpenXml.Word

open System.IO
open DocumentFormat.OpenXml.Packaging
open MinMe.Core

let processWordStream (stream:Stream) contentInfo =
    use doc = WordprocessingDocument.Open(stream, false)
    {
        contentInfo with
            NumberOfImages =
                doc.MainDocumentPart.ImageParts |> Seq.length
    }

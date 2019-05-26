module MinMe.Core.OpenXml.Word

open System.IO
open DocumentFormat.OpenXml.Packaging
open MinMe.Core.Model

let processWordStream (stream:Stream) contentInfo =
    use doc = WordprocessingDocument.Open(stream, false)
    contentInfo

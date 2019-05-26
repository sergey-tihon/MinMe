namespace MinMe.Core

[<AutoOpen>]
module Helpers =

    let printFileSize (size:int64) =
        let rec loop size suffixes =
            if size < 1024.0
            then sprintf "%.1f %s" size (List.head suffixes)
            else loop (size/1024.0) (List.tail suffixes)
        loop (double size) ["bytes"; "kB"; "MB"; "GB"; "TB"]

    open DocumentFormat.OpenXml.Packaging

    let getPartSize (part:OpenXmlPart) =
        use stream = part.GetStream()
        stream.Length

    let getDataPartSize (part:DataPart) =
        use stream = part.GetStream()
        stream.Length

[<RequireQualifiedAccess>]
module Option =
    open DocumentFormat.OpenXml

    let ofStringValue (obj:StringValue) =
        if obj.HasValue then obj.Value |> Option.ofObj else None
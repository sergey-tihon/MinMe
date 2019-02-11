module MinMe.OpenXml.PowerPoint

open System.IO
open DocumentFormat.OpenXml.Packaging
open MinMe.Core

let processPowerPointStream (stream:Stream) contentInfo =
    use doc = PresentationDocument.Open(stream, false)
    {
        contentInfo with
            NumberOfImages =
                doc.PresentationPart.SlideParts
                |> Seq.sumBy (fun s -> s.ImageParts |> Seq.length)
            Images  =
                doc.PresentationPart.SlideParts
                |> Seq.collect (fun s ->
                    s.ImageParts
                    |> Seq.map (fun x ->
                        let length =
                            use stream = x.GetStream()
                            stream.Length
                        x.ContentType, length))
                |> Seq.groupBy (fst)
                |> Seq.map (fun (k,s) ->
                    let size = s |> Seq.sumBy snd
                    k, Seq.length s, size)
                |> Seq.sortBy (fun (_,_,s) -> -s)
                |> Seq.map (fun (cTy, cnt, size) -> sprintf "%s (%d images, %s)" cTy cnt (printFileSize size))
                |> List.ofSeq
            Videos =
                doc.PresentationPart.SlideParts
                |> Seq.collect (fun slide ->
                    slide.DataPartReferenceRelationships
                    |> Seq.choose (function
                        | :? VideoReferenceRelationship as vid ->
                            let length =
                                use stream = vid.DataPart.GetStream()
                                stream.Length
                            let msg = sprintf "%s (%s)" (vid.Uri.OriginalString) (printFileSize length)
                            Some (length, msg)
//                        | :? MediaReferenceRelationship as med ->
//                            let length =
//                                use stream = med.DataPart.GetStream()
//                                stream.Length
//                            let msg = sprintf "%s (%s)" (med.Uri.OriginalString) (printFileSize length)
//                            Some (length, msg)
                        | _ -> None)
                )
                |> Seq.sortBy (fun (s,_) -> -s)
                |> Seq.distinct
                |> Seq.map (snd)
                |> List.ofSeq
    }


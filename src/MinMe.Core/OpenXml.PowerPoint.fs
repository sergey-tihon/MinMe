module MinMe.OpenXml.PowerPoint

open System.IO
open DocumentFormat.OpenXml.Packaging
open MinMe.Model


let getPartSize (part:OpenXmlPart) =
    use stream = part.GetStream()
    stream.Length
let getDataPartSize (part:DataPart) =
    use stream = part.GetStream()
    stream.Length

let processPowerPointStream (stream:Stream) contentInfo =
    let visitedParts = System.Collections.Generic.HashSet<_>()
    let rec processOpenXmlPart (root:OpenXmlPart) =
        let key = root.Uri.OriginalString
        if visitedParts.Contains key
        then Seq.empty
        else 
            visitedParts.Add key |> ignore
            seq {
                yield {
                    Name = key
                    PartType = root.GetType().Name
                    ContentType = root.ContentType
                    Size = getPartSize root
                }
                for ref in root.DataPartReferenceRelationships do
                    let dataPart = ref.DataPart
                    let dataPartKey = dataPart.Uri.OriginalString
                    if not <| visitedParts.Contains dataPartKey then
                        visitedParts.Add dataPartKey |> ignore
                        yield {
                            Name = dataPartKey
                            PartType = dataPart.GetType().Name
                            ContentType = dataPart.ContentType
                            Size = getDataPartSize dataPart
                        }

                for pair in root.Parts do
                    yield! processOpenXmlPart pair.OpenXmlPart
            }

    use doc = PresentationDocument.Open(stream, false)
    {
        contentInfo with
            Parts =
                doc.Parts
                |> Seq.collect (fun pair -> processOpenXmlPart pair.OpenXmlPart)
                |> Seq.sortBy (fun x -> x.PartType)
                |> Seq.toList
    }


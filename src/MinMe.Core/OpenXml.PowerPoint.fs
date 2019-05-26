module MinMe.Core.OpenXml.PowerPoint


open System
open System.IO
open System.Collections.Generic
open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Presentation
open MinMe.Core
open MinMe.Core.Model

let private enumerateAllParts (doc:PresentationDocument) =
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

    doc.Parts
    |> Seq.collect (fun pair -> processOpenXmlPart pair.OpenXmlPart)
    |> Seq.sortBy (fun x -> x.PartType)
    |> Seq.toList


let private getPartUsageData (doc:PresentationDocument) :Map<string, List<PartUsageInfo>> =
    let dict = Dictionary<string, List<PartUsageInfo>>()
    let addUsage (uri:Uri) usage =
        let key = uri.OriginalString
        match dict.TryGetValue key with
        | true, list -> list.Add usage
        | false, _ -> dict.Add(key, List<_>([usage]))

    let presentation = doc.PresentationPart
    let getPart (relId:StringValue) =
        Option.ofStringValue  relId
        |> Option.map presentation.GetPartById

    let slideIds = presentation.Presentation.SlideIdList.ChildElements.OfType<SlideId>();
    for elem in slideIds do
        match getPart elem.RelationshipId with
        | None -> () // this is strange
        | Some(part) ->
            addUsage part.Uri (Rerefence presentation.Uri)

    dict
    |> Seq.map (fun x -> x.Key, x.Value)
    |> Map.ofSeq


let processPowerPointStream (stream:Stream) contentInfo =
    use doc = PresentationDocument.Open(stream, false)
    {
        contentInfo with
            Parts = enumerateAllParts doc
            PartUsages = getPartUsageData doc
    }


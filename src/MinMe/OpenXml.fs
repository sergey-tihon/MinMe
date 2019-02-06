module MinMe.OpenXml

open System.IO
open DocumentFormat.OpenXml.Packaging


let private printFileSize (size:int64) =
    let rec loop size suffixes =
        if size < 1024.0
        then sprintf "%.1f %s" size (List.head suffixes)
        else loop (size/1024.0) (List.tail suffixes)
    loop (double size) ["bytes"; "kB"; "MB"; "GB"; "TB"]

type FileContentInfo = 
    {
        FileName : string
        FileSize : int64
        NumberOfImages : int
    }
    override this.ToString() =
        sprintf "%s; %d images" 
            (printFileSize this.FileSize) 
            this.NumberOfImages
    static member Default = {
        FileName = ""
        FileSize = 0L
        NumberOfImages = 0
    }


let processPowerPointStream (stream:Stream) contentInfo =
    use doc = PresentationDocument.Open(stream, false)
    { 
        contentInfo with
            NumberOfImages = 
                doc.PresentationPart.SlideParts 
                |> Seq.sumBy (fun s -> s.ImageParts |> Seq.length)
    }

let processWordStream (stream:Stream) contentInfo =
    use doc = WordprocessingDocument.Open(stream, false)
    { 
        contentInfo with
            NumberOfImages = 
                doc.MainDocumentPart.ImageParts |> Seq.length
    }

let processFile fileName = 
    use fs = File.Open(fileName, FileMode.Open)
    let contentInfo = {
        FileContentInfo.Default with
            FileName = fileName
            FileSize = fs.Length
    }
    match Path.GetExtension(fileName).ToLowerInvariant() with // TODO: Identify by content type
    | ".pptx" -> contentInfo |> processPowerPointStream fs
    | ".docx" -> contentInfo |> processWordStream fs
    | ext -> failwithf "File type '%s' is not supported" ext
    
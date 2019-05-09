module MinMe.Model

let printFileSize (size:int64) =
    let rec loop size suffixes =
        if size < 1024.0
        then sprintf "%.1f %s" size (List.head suffixes)
        else loop (size/1024.0) (List.tail suffixes)
    loop (double size) ["bytes"; "kB"; "MB"; "GB"; "TB"]

type PartInfo =
    {
        Name : string
        PartType : string
        ContentType : string
        Size : int64
    }

type FileContentInfo =
    {
        FileName : string
        FileSize : int64
        Parts : PartInfo list
    }
    override this.ToString() =
        sprintf "%s" (printFileSize this.FileSize)

    static member Default = {
        FileName = ""
        FileSize = 0L
        Parts = []
    }
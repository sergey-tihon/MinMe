module MinMe.Core

let printFileSize (size:int64) =
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
        Images : string list
        Videos: string list
    }
    override this.ToString() =
        sprintf "%s; %d images"
            (printFileSize this.FileSize)
            this.NumberOfImages

    static member Default = {
        FileName = ""
        FileSize = 0L
        NumberOfImages = 0
        Images = []
        Videos = []
    }
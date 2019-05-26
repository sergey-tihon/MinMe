module MinMe.Core.Model

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
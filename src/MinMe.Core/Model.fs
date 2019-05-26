module MinMe.Core.Model

open System
open System.Collections.Generic

type PartInfo =
    {
        Name : string
        PartType : string
        ContentType : string
        Size : int64
    }

type ImageUsageInfo =
    {
        Width : int
        Height : int
        CropX : int
        CropY : int
    }

type PartUsageInfo =
    | Rerefence of from:Uri
    | ImageUsage of ImageUsageInfo

type FileContentInfo =
    {
        FileName : string
        FileSize : int64

        Parts : PartInfo list
        PartUsages : Map<string, List<PartUsageInfo>>
    }
    override this.ToString() =
        sprintf "%s" (printFileSize this.FileSize)

    static member Default = {
        FileName = ""
        FileSize = 0L
        Parts = []
        PartUsages = Map.empty
    }
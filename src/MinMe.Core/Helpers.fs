namespace MinMe.Core

[<AutoOpen>]
module Helpers =

    let printFileSize (size:int64) =
        let rec loop size suffixes =
            if size < 1024.0
            then sprintf "%.1f %s" size (List.head suffixes)
            else loop (size/1024.0) (List.tail suffixes)
        loop (double size) ["bytes"; "kB"; "MB"; "GB"; "TB"]

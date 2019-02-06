namespace MinMe.macOS

open System
open System.Threading.Tasks
open AppKit
open Xamarin.Forms

type MacOsPlatform (window:NSWindow) =
    member this.UpdateDock() =
        let dock = NSApplication.SharedApplication.DockTile
        dock.BadgeLabel <- "wait..."

    interface MinMe.App.IPlatformContract with
        member this.ChooseFile fileTypes =
            let dlg = NSOpenPanel.OpenPanel //new NSOpenPanel()
            dlg.Prompt <- "Select file for optimize"
            dlg.WorksWhenModal <- true
            dlg.AllowsMultipleSelection <- false
            dlg.CanChooseFiles <- true
            dlg.CanChooseDirectories <- false
            dlg.ResolvesAliases <-  true
            dlg.AllowedFileTypes <- fileTypes

            let tcs = new TaskCompletionSource<string>()
            dlg.BeginSheet(window, NSSavePanelComplete(fun (res:nint) ->
                if res <> nint 1
                then tcs.SetResult(null)
                else
                    let file = dlg.Urls.[0]
                    tcs.SetResult(file.Path)
                    //this.UpdateDock()
            ))

            tcs.Task |> Async.AwaitTask
            //if dlg.RunModal() = nint 1
            //then dlg.Urls |> Array.map (fun x->x.Path)
            //else [||]

        member this.GetIconForFileType fileType =
            let icon = NSWorkspace.SharedWorkspace.IconForFileType fileType
            let data = icon.AsTiff()
            let bytes :byte[] = Array.zeroCreate((int)data.Length)
            System.Runtime.InteropServices.Marshal.Copy(data.Bytes, bytes, 0, Convert.ToInt32(data.Length))
            ImageSource.FromStream(fun () -> new IO.MemoryStream(bytes) :> IO.Stream)
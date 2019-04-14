namespace MinMe.macOS

open System
open System.Threading.Tasks
open AppKit
open AppKit
open AppKit
open Xamarin.Forms

type ProgressAlert() as this =
    inherit NSAlert()
    let progressBar = NSProgressIndicator()
    do
        this.MessageText <- "Loading..."
        this.InformativeText <- "Please wait"
        let frame = CoreGraphics.CGRect(nfloat 0.f, nfloat 0.f, nfloat 290.f, nfloat 16.f)
        this.AccessoryView <- NSView(frame)
        this.AccessoryView.AddSubview(progressBar)
        //this.Self.layout()
        let point = CoreGraphics.CGPoint(this.AccessoryView.Frame.X, this.Window.Frame.Y)
        this.AccessoryView.SetFrameOrigin(point)
        //this.AccessoryView.SetFrameOrigin(NSPoint(x:(self.AccessoryView.Frame)!.minX,y:self.window.frame.maxY))
        this.AddButton("Quit")

        progressBar.Indeterminate <- true
        progressBar.Style <- NSProgressIndicatorStyle.Bar
        progressBar.SizeToFit()
        progressBar.SetFrameSize(CoreGraphics.CGSize(nfloat 290.f, nfloat 16.f))
        progressBar.UsesThreadedAnimation <- true
        progressBar.StartAnimation(null)

    member this.UpdateMessage (msg)  =
        this.InformativeText <- msg

//type ProgressWnd() as this =
//    inherit NSWindow()
//    let progressBar = NSProgressIndicator()
//    do
//        let frame = CoreGraphics.CGRect(nfloat 0.f, nfloat 20.f, nfloat 120.f, nfloat 120.f)
//        //this.ContentView <- NSView(frame)
//        this.SetFrame(frame, true)
//        this.ContentView.AddSubview(progressBar)
//
//        progressBar.Style <- NSProgressIndicatorStyle.Spinning
//        progressBar.SetFrameSize(CoreGraphics.CGSize(nfloat 100.f, nfloat 100.f))
//        //progressBar.SizeToFit()
//        progressBar.AutoresizingMask <- NSViewResizingMask.WidthSizable ||| NSViewResizingMask.HeightSizable
//        progressBar.Indeterminate <- true
//        progressBar.UsesThreadedAnimation <- true
//        progressBar.StartAnimation(null)

type MacOsPlatform (window:NSWindow) =
    let loading = ProgressAlert()

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

        member this.ShowProgress () =
            loading.BeginSheet(window)
            loading.UpdateMessage

        member this.HideProgress () =
            window.EndSheet(loading.Window)

namespace MinMe.macOS

open System
open AppKit
open Foundation
open Xamarin.Forms
open Xamarin.Forms.Platform.MacOS

[<Register("AppDelegate")>]
type AppDelegate() =
    inherit FormsApplicationDelegate()
    let style = NSWindowStyle.Closable ||| NSWindowStyle.Titled ||| NSWindowStyle.Resizable
    let rect = new CoreGraphics.CGRect(nfloat 200.0, nfloat 300.0, nfloat 885.0, nfloat 600.0)
    let window = new NSWindow(rect, style, NSBackingStore.Buffered, false, Title = "MinMe")

    override __.MainWindow = window

    override this.DidFinishLaunching(notification: NSNotification) =
        Forms.Init()
        let platform = MacOsPlatform(window)
        this.LoadApplication(new MinMe.App(platform))
        base.DidFinishLaunching(notification)

    override __.WillTerminate(notification: NSNotification) =
        // Insert code here to tear down your application
        ()

module EntryClass =
    [<EntryPoint>]
    let Main(args: string[]) =
        NSApplication.Init() |> ignore
        NSApplication.SharedApplication.Delegate <- new AppDelegate()
        NSApplication.Main(args)
        0

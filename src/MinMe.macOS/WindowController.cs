// This file has been autogenerated from a class added in the UI designer.

using System;

using AppKit;

using MinMe.Core;
using MinMe.macOS.Views;

namespace MinMe.macOS
{
	public partial class WindowController : NSWindowController
	{
		public WindowController (IntPtr handle) : base (handle)
		{
            progress = new ProgressAlert();
		}

        private readonly string[] fileTypes = { "pptx" };
        private ProgressAlert progress;

        partial void OpenFile(AppKit.NSToolbarItem sender)
        {
            var dlg = NSOpenPanel.OpenPanel; //new NSOpenPanel()
            dlg.Prompt = "Select file for optimization";
            dlg.WorksWhenModal = true;
            dlg.AllowsMultipleSelection = false;
            dlg.CanChooseFiles = true;
            dlg.CanChooseDirectories = false;
            dlg.ResolvesAliases = true;
            dlg.AllowedFileTypes = fileTypes;

            dlg.BeginSheet(Window, async res => {
                if (res != 1) return;

                progress.BeginSheet(this.Window);

                var file = dlg.Urls[0].Path;
                var result = await Agents.analyze(file);
                var c = this.ContentViewController as ViewController;
                c?.InitializeView(result);

                this.Window.EndSheet(progress.Window);
            });
        }

        partial void Optimize(Foundation.NSObject sender)
        {

        }
    }
}

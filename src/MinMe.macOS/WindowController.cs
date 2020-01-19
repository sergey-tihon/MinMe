// This file has been autogenerated from a class added in the UI designer.

using System;
using System.IO;
using System.Threading.Tasks;

using AppKit;
using Clippit;
using Clippit.PowerPoint;
using MinMe.Core.PowerPoint;
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

                var fileName = dlg.Urls[0].Path;
                var result = await Task.Run(() =>
                {
                    var analyzer = new PowerPointAnalyzer(fileName);
                    return analyzer.Analyze();
                });
                var c = this.ContentViewController as ViewController;
                c?.InitializeView(result);

                this.Window.EndSheet(progress.Window);
            });
        }

        partial void Optimize(Foundation.NSObject sender)
        {

        }

        partial void Publish(Foundation.NSObject sender)
        {
            var dlg = NSOpenPanel.OpenPanel; //new NSOpenPanel()
            dlg.Prompt = "Select file for publishing";
            dlg.WorksWhenModal = true;
            dlg.AllowsMultipleSelection = false;
            dlg.CanChooseFiles = true;
            dlg.CanChooseDirectories = false;
            dlg.ResolvesAliases = true;
            dlg.AllowedFileTypes = fileTypes;

            dlg.BeginSheet(Window, async res => {
                if (res != 1) return;

                progress.BeginSheet(this.Window);

                var fileName = dlg.Urls[0].Path;
                await Task.Run(() =>
                {
                    var presentation = new PmlDocument(fileName);
                    var slides = PresentationBuilder.PublishSlides(presentation);
                    var targetDir = new FileInfo(fileName).DirectoryName;
                    foreach (var slide in slides)
                    {
                        var targetPath = Path.Combine(targetDir, Path.GetFileName(slide.FileName));
                        slide.SaveAs(targetPath);
                    }
                });

                this.Window.EndSheet(progress.Window);
            });
        }
    }
}

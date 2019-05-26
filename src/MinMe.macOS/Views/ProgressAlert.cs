using System;
using AppKit;

namespace MinMe.macOS.cs.Views
{
    public class ProgressAlert : NSAlert
    {
        public ProgressAlert()
        {
            progressBar = new NSProgressIndicator();

            MessageText = "Loading...";
            InformativeText = "Please wait";

            var frame = new CoreGraphics.CGRect(0, 0, 290, 16);
            AccessoryView = new NSView(frame);
            AccessoryView.AddSubview(progressBar);

            var point = new CoreGraphics.CGPoint(AccessoryView.Frame.X, Window.Frame.Y);
            AccessoryView.SetFrameOrigin(point);
            AddButton("Cancel");

            progressBar.Indeterminate = true;
            progressBar.Style = NSProgressIndicatorStyle.Bar;
            progressBar.SizeToFit();
            progressBar.SetFrameSize(new CoreGraphics.CGSize(290, 16));
            progressBar.UsesThreadedAnimation = true;
            progressBar.StartAnimation(null);
        }

        private readonly NSProgressIndicator progressBar;

        public void UpdateMessage(string msg) => InformativeText = msg;
    }
}

using System;

using AppKit;

using Foundation;

using MinMe.Core;
using MinMe.Core.Model;
using MinMe.macOS.Data;

namespace MinMe.macOS
{
    public partial class ViewController : NSViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }


        public void InitializeView (FileContentInfo model)
        {
            FileName.StringValue = System.IO.Path.GetFileName(model.FileName);
            FileSize.StringValue = Helpers.PrintFileSize(model.FileSize);

            var partsSource = new ImageListDataSource(model.Parts, model);
            PartsTable.DataSource = partsSource;
            PartsTable.Delegate = new ImageListDelegate(partsSource);
        }


        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Do any additional setup after loading the view.
        }

        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }
            set
            {
                base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
        }
    }
}

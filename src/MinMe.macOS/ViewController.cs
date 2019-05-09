using System;

using AppKit;
using Foundation;
using MinMe.macOS.cs.Data;

namespace MinMe.macOS.cs
{
    public partial class ViewController : NSViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }


        public void InitializeView (Model.FileContentInfo model)
        {
            FileName.StringValue = System.IO.Path.GetFileName(model.FileName);
            FileSize.StringValue = Model.printFileSize(model.FileSize);

            var partsSource = new ImageListDataSource(model.Parts);
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

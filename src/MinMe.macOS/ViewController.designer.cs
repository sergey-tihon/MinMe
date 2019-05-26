// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//

using Foundation;

namespace MinMe.macOS
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		AppKit.NSTextField FileName { get; set; }

		[Outlet]
		AppKit.NSTextField FileSize { get; set; }

		[Outlet]
		AppKit.NSTableView PartsTable { get; set; }

		[Action ("ClickedButton:")]
		partial void ClickedButton (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (FileName != null) {
				FileName.Dispose ();
				FileName = null;
			}

			if (FileSize != null) {
				FileSize.Dispose ();
				FileSize = null;
			}

			if (PartsTable != null) {
				PartsTable.Dispose ();
				PartsTable = null;
			}
		}
	}
}

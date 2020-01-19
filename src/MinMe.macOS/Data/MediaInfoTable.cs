using System;
using System.Collections.Generic;

using AppKit;

using Foundation;

using MinMe.Core;
using MinMe.Core.Model;

namespace MinMe.macOS.Data
{
    public class ImageListDataSource : NSTableViewDataSource
    {
        public ImageListDataSource(IEnumerable<PartInfo> items, FileContentInfo model)
        {
            _model = model;
            Items.AddRange(items);
        }

        private readonly FileContentInfo _model;
        public readonly List<PartInfo> Items = new List<PartInfo>();

        public override nint GetRowCount(NSTableView tableView)
        {
            return Items.Count;
        }

        public List<PartUsageInfo> GetUsage(PartInfo info)
            => _model.PartUsages.TryGetValue(info.Name, out var usage) ? usage : null;

        public void Sort(string key, bool ascending)
        {

            // Take action based on key
            switch (key)
            {
                case "Name":
                    if (ascending)
                        Items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase));
                    else
                        Items.Sort((x, y) => -1 * string.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase));
                    break;
                case "Type":
                    if (ascending)
                        Items.Sort((x, y) => string.Compare(x.PartType, y.PartType, StringComparison.InvariantCultureIgnoreCase));
                    else
                        Items.Sort((x, y) => -1 * string.Compare(x.PartType, y.PartType, StringComparison.InvariantCultureIgnoreCase));
                    break;
                case "ContentType":
                    if (ascending)
                        Items.Sort((x, y) => string.Compare(x.ContentType, y.ContentType, StringComparison.InvariantCultureIgnoreCase));
                    else
                        Items.Sort((x, y) => -1 * string.Compare(x.ContentType, y.ContentType, StringComparison.InvariantCultureIgnoreCase));
                    break;
                case "Size":
                    if (ascending)
                        Items.Sort((x, y) => x.Size.CompareTo(y.Size));
                    else
                        Items.Sort((x, y) => -1 * x.Size.CompareTo(y.Size));
                    break;
                case "Used":
                    Items.Sort((x, y) =>
                    {
                        var xx = GetUsage(x)?.Count ?? 0;
                        var yy = GetUsage(y)?.Count ?? 0;
                        var cmp = xx.CompareTo(yy);
                        return ascending ? cmp : -cmp;
                    });
                    break;
            }

        }

        public override void SortDescriptorsChanged(NSTableView tableView, NSSortDescriptor[] oldDescriptors)
        {
            // Sort the data
            //if (oldDescriptors.Length > 0)
            //{
            //    // Update sort
            //    Sort(oldDescriptors[0].Key, oldDescriptors[0].Ascending);
            //}
            //else
            {
                // Grab current descriptors and update sort
                NSSortDescriptor[] tbSort = tableView.SortDescriptors;
                Sort(tbSort[0].Key, tbSort[0].Ascending);
            }

            // Refresh table
            tableView.ReloadData();
        }
    }

    public class ImageListDelegate : NSTableViewDelegate 
    {
        private const string CellIdentifier = "ImagePartCell";
        private readonly ImageListDataSource _dataSource;

        public ImageListDelegate(ImageListDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public override NSView GetViewForItem(NSTableView tableView, NSTableColumn tableColumn, nint row)
        {
            // This pattern allows you reuse existing views when they are no-longer in use.
            // If the returned view is null, you instance up a new view.
            // If a non-null view is returned, you modify it enough to reflect the new data.
            var view = (NSTextField) tableView.MakeView(CellIdentifier, this)
                       ?? new NSTextField {Identifier = CellIdentifier};

            view.BackgroundColor = NSColor.Clear;
            view.TextColor = NSColor.Text;
            view.Bordered = false;
            view.Selectable = false;
            view.Editable = false;

            // Set up view based on the column and row
            var item = _dataSource.Items[(int) row];
            switch (tableColumn.Title)
            {
                case "Name":
                    view.StringValue = item.Name;
                    break;
                case "Type":
                    if (item.PartType == "ExtendedPart")
                    {
                        // Type="http://schemas.microsoft.com/office/2007/relationships/hdphoto" Target="media/hdphoto1.wdp"
                        view.TextColor = NSColor.Red;
                    }
                    view.StringValue = item.PartType;
                    break;
                case "Content Type":
                    view.StringValue = item.ContentType;
                    break;
                case "Size":
                    if (item.Size > 1_000_000)
                    {
                        view.TextColor = NSColor.Red;
                    }
                    view.StringValue = Helpers.PrintFileSize(item.Size);
                    break;
                case "Used":
                    var data = "";
                    var usage = _dataSource.GetUsage(item);
                    if (usage != null)
                    {
                        data = (usage.Count > 1)
                            ? $"Yes ({usage.Count})"
                            : "Yes";
                    }
                    view.StringValue = data;
                    break;
            }

            return view;
        }

        private NSData GetFileIcon(string fileType)
        {
            var icon = NSWorkspace.SharedWorkspace.IconForFileType(fileType);
            var data = icon.AsTiff();
            return data;
            //var bytes = new int[data.Length];
            //System.Runtime.InteropServices.Marshal.Copy(data.Bytes, bytes, 0, Convert.ToInt32(data.Length));
            //return ImageSource.FromStream(() => new IO.MemoryStream(bytes));
        }
    }
}

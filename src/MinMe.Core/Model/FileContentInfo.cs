using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MinMe.Core.Model
{
    public class FileContentInfo
    {
        public FileContentInfo(string fileName, long fileSize)
        {
            FileName = fileName;
            FileSize = fileSize;

            Parts = new List<PartInfo>();
            PartUsages = new Dictionary<string, List<PartUsageInfo>>();
        }

        public string FileName { get; }
        public long FileSize { get; }

        public List<PartInfo> Parts { get; set; }
        public Dictionary<string, List<PartUsageInfo>> PartUsages { get; set; }

        public override string ToString()
            => Helpers.PrintFileSize(FileSize);
    }
}

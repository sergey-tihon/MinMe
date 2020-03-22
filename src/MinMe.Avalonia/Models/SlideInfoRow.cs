using MinMe.Analyzers.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinMe.Avalonia.Models
{
    class SlideInfoRow
    {
        private readonly SlideInfo _slideInfo;
        public SlideInfoRow(SlideInfo slideInfo, long size) =>
            (_slideInfo, Size) = (slideInfo, size);

        public int Number => _slideInfo.Number;
        public string FileName => _slideInfo.FileName;
        public string Title => _slideInfo.Title;
        public long Size { get; }
    }
}

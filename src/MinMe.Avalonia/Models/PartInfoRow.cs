using MinMe.Analyzers.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinMe.Avalonia.Models
{
    class PartInfoRow
    {
        private readonly PartInfo _partInfo;
        public PartInfoRow(PartInfo partInfo, string usage) =>
            (_partInfo, Usage) = (partInfo, usage);

        public string Name => _partInfo.Name;
        public string PartType => _partInfo.PartType;
        public string ContentType => _partInfo.ContentType;
        public long Size => _partInfo.Size;
        public string Usage { get; }
    }
}

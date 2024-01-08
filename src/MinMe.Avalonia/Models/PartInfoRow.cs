using MinMe.Analyzers.Model;

namespace MinMe.Avalonia.Models;

class PartInfoRow(PartInfo partInfo, string usage)
{
    public string Name => partInfo.Name;
    public string PartType => partInfo.PartType;
    public string ContentType => partInfo.ContentType;
    public long Size => partInfo.Size;
    public string Usage { get; } = usage;
}

using MinMe.Analyzers.Model;

namespace MinMe.Avalonia.Models;

class SlideInfoRow(SlideInfo slideInfo, long size)
{
    public int Number => slideInfo.Number;
    public string FileName => slideInfo.FileName;
    public string Title => slideInfo.Title;
    public long Size { get; } = size;
}

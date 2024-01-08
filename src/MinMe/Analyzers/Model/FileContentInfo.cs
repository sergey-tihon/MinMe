namespace MinMe.Analyzers.Model;

public class FileContentInfo(string fileName, long fileSize)
{
    public string FileName { get; } = fileName;
    public long FileSize { get; } = fileSize;

    public List<PartInfo> Parts { get; set; } = [];
    public Dictionary<string, List<PartUsageInfo>> PartUsages { get; set; } = new();

    // TODO: Refactor
    public List<SlideInfo> Slides { get; set; } = [];

    public override string ToString() => 
        Helpers.PrintFileSize(FileSize);
}

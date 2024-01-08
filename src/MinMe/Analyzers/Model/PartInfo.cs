namespace MinMe.Analyzers.Model;

public class PartInfo(string name, string partType, string contentType, long size)
{
    public string Name { get; } = name;
    public string PartType { get; } = partType;
    public string ContentType { get; } = contentType;
    public long Size { get; } = size;
}

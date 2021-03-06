namespace MinMe.Analyzers.Model
{
    public class PartInfo
    {
        public PartInfo(string name, string partType, string contentType, long size) =>
            (Name, PartType, ContentType, Size) = (name, partType, contentType, size);

        public string Name { get; }
        public string PartType { get; }
        public string ContentType { get; }
        public long Size { get; }
    }
}
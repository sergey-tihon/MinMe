namespace MinMe.Core.Model
{
    public class PartInfo
    {
        public PartInfo(string name, string partType, string contentType, long size)
        {
            Name = name;
            PartType = partType;
            ContentType = contentType;
            Size = size;
        }

        public string Name { get; }
        public string PartType { get; }
        public string ContentType { get; }
        public long Size { get; }
    }
}
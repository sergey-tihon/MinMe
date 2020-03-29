namespace MinMe.Tests.RepoTests
{
    public class OptimizeResult
    {
        public string FileName { get; set; }
        public long FileSizeBefore { get; set; }
        public long FileSizeAfter { get; set; }
        public double Compression { get; set; }
    }
}
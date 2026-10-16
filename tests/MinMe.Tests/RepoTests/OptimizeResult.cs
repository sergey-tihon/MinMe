using System.Text.Json.Serialization;

namespace MinMe.Tests.RepoTests;

public class OptimizeResult
{
    public string FileName { get; set; } = String.Empty;
    public long FileSizeBefore { get; set; }
    public long FileSizeAfter { get; set; }

    [JsonIgnore]
    public double Compression => 100.0 * (1.0 - FileSizeAfter / FileSizeBefore);

    public List<string>? Errors { get; set; }
}

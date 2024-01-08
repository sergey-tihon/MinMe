using System.Text.Json.Serialization;

namespace MinMe.Tests.RepoTests;

public class OptimizeResult
{
    public string FileName { get; set; } = String.Empty;
    public long FileSizeBefore { get; set; }

    [JsonPropertyName(nameof(FileSizeAfter))]
    public Dictionary<string, long> FileSizeAfterOnOs { get; set; } = new();

    [JsonIgnore]
    public long FileSizeAfter
    {
        get => FileSizeAfterOnOs.GetValueOrDefault(OsMoniker, FileSizeBefore);
        set => FileSizeAfterOnOs[OsMoniker] = value;
    }

    [JsonIgnore]
    public double Compression =>
        100.0 * (1.0 - FileSizeAfter / FileSizeBefore);
    public List<string>? Errors { get; set; }

    public static string OsMoniker =>
        OperatingSystem.IsWindows() ? "win" : "macOS";
}
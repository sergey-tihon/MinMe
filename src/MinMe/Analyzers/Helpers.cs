using DocumentFormat.OpenXml.Packaging;

namespace MinMe.Analyzers;

public static class Helpers
{
    private static readonly string[] FileSizeOrders = { "B", "KB", "MB", "GB", "TB" };

    public static string PrintFileSize(double size)
    {
        var order = 0;
        while (size >= 1024 && order < FileSizeOrders.Length - 1) {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {FileSizeOrders[order]}";
    }

    public static long GetPartSize(this OpenXmlPart part)
    {
        using var stream = part.GetStream();
        return stream.Length;
    }

    public static long GetPartSize(this DataPart part)
    {
        using var stream = part.GetStream();
        return stream.Length;
    }
}
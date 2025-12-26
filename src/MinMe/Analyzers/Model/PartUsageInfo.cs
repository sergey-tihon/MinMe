namespace MinMe.Analyzers.Model;

public class PartUsageInfo;

public class Reference(Uri from) : PartUsageInfo
{
    public Uri From { get; } = from;
}

public class ImageUsage(ImageUsageInfo imageUsageInfo, Uri from) : Reference(from)
{
    public ImageUsageInfo ImageUsageInfo { get; } = imageUsageInfo;
}

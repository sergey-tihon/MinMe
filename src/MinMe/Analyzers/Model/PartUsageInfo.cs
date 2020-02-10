using System;

namespace MinMe.Analyzers.Model
{
    public class PartUsageInfo
    {
    }

    public class Reference : PartUsageInfo
    {
        public Reference(Uri from) => From = from;
        public Uri From { get; }
    }

    public class ImageUsage : PartUsageInfo
    {
        public ImageUsage(ImageUsageInfo imageUsageInfo) => ImageUsageInfo = imageUsageInfo;
        public ImageUsageInfo ImageUsageInfo { get; }
    }
}
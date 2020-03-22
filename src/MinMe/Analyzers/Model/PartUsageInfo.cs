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

    public class ImageUsage : Reference
    {
        public ImageUsage(ImageUsageInfo imageUsageInfo, Uri from) : base(from)
            => ImageUsageInfo = imageUsageInfo;
        public ImageUsageInfo ImageUsageInfo { get; }
    }
}
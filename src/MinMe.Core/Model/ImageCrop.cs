using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;

namespace MinMe.Core.Model
{
    public class ImageCrop
    {
        public ImageCrop(int left, int right, int top, int bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public int Left { get; }
        public int Right { get; }
        public int Top { get; }
        public int Bottom { get; }

        public static ImageCrop? FromSourceRect(SourceRectangle? srcRect) =>
            srcRect is null
                ? null
                : new ImageCrop(
                    srcRect.Left ?? 0, srcRect.Right ?? 0,
                    srcRect.Top ?? 0, srcRect.Bottom ?? 0);
    }
}
using DocumentFormat.OpenXml.Drawing;

namespace MinMe.Analyzers.Model;

public class ImageCrop
{
    private ImageCrop(int left, int right, int top, int bottom) =>
        (Left, Right, Top, Bottom) = (left, right, top, bottom);

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
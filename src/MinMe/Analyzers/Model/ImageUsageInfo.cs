using DocumentFormat.OpenXml.Presentation;

namespace MinMe.Analyzers.Model;

public class ImageUsageInfo(long width, long height, ImageCrop? crop)
{
    public long Width { get; } = width;
    public long Height { get; } = height;
    public ImageCrop? Crop { get; } = crop;

    public static ImageUsageInfo FromPict(Picture pict)
    {
        var crop = ImageCrop.FromSourceRect(pict.BlipFill?.SourceRectangle);
        if (pict.ShapeProperties?.Transform2D?.Extents is { Cx: { } cx, Cy: { } cy })
            return new ImageUsageInfo(cx.Value, cy.Value, crop);
        return new ImageUsageInfo(0, 0, crop);
    }
}

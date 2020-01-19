using DocumentFormat.OpenXml.Drawing;

namespace MinMe.Core.Model
{
    public class ImageUsageInfo
    {
        public ImageUsageInfo(long width, long height, ImageCrop? crop)
        {
            Width = width;
            Height = height;
            Crop = crop;
        }

        public long Width { get; }
        public long Height { get; }
        public ImageCrop? Crop { get; }

        public static ImageUsageInfo FromPict(Picture pict)
        {
            var crop = ImageCrop.FromSourceRect(pict.BlipFill?.SourceRectangle);
            if (pict.ShapeProperties.Transform2D is {} transform)
                return new ImageUsageInfo(
                    transform.Extents.Cx.Value,
                    transform.Extents.Cy.Value,
                    crop);
            return new ImageUsageInfo(0, 0, crop);
        }
    }
}
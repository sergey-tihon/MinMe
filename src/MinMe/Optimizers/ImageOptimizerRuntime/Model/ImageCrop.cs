using System.Drawing;

using DocumentFormat.OpenXml.Drawing;

using Rectangle = System.Drawing.Rectangle;

namespace MinMe.Optimizers.ImageOptimizerRuntime.Model
{
    public class ImageCrop: IEquatable<ImageCrop>
    {
        public long Left { get; }
        public long Right { get; }
        public long Top { get; }
        public long Bottom { get; }

        private SourceRectangle? SourceRectangle { get;}
        private DocumentFormat.OpenXml.Vml.ImageData? ImageData { get; }

        public ImageCrop(SourceRectangle? sourceRectangle,
                         DocumentFormat.OpenXml.Vml.ImageData? imageData,
                         long left, long right, long top, long bottom)
        {
            SourceRectangle = sourceRectangle;
            ImageData = imageData;
            (Left, Right, Top, Bottom) = (left, right, top, bottom);
        }

        public bool Equals(ImageCrop? other) =>
            (Left, Right, Top, Bottom) == (other?.Left, other?.Right, other?.Top, other?.Bottom);

        public override int GetHashCode() =>
            Left.GetHashCode() ^ Right.GetHashCode() ^ Top.GetHashCode() ^ Bottom.GetHashCode();

        public Rectangle GetRectangle(Size size)
        {
            var x = Percentage(size.Width, Left);
            var y = Percentage(size.Height, Top);
            var w = size.Width - Percentage(size.Width, Right + Left);
            var h = size.Height - Percentage(size.Height, Bottom + Top);
            return new Rectangle(x, y, w, h);
        }

        private static int Percentage(int value, long percentage1000)
            => (int) (value * percentage1000 / 100_000);

        public void RemoveCrop()
        {
            SourceRectangle?.Remove();

            if (ImageData is null) return;
            ImageData.CropLeft = "";
            ImageData.CropRight = "";
            ImageData.CropTop = "";
            ImageData.CropBottom = "";
        }

        public bool IsValid() =>
            Left >= 0 && Right >= 0 && Top >= 0 && Bottom >= 0 &&
            Left + Right <= 100_000 && Top + Bottom <= 100_000;
    }
}

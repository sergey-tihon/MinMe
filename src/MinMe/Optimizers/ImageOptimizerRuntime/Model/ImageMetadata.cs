using System.Drawing;

using DocumentFormat.OpenXml.Packaging;

namespace MinMe.Optimizers.ImageOptimizerRuntime.Model
{
    internal class ImageMetadata
    {
        /// <summary>
        /// OpenXML image part from Office document (contains image binary)
        /// </summary>
        public ImagePart ImagePart { get; }
        /// <summary>
        /// Image sizes found in document markup
        /// </summary>
        public List<Size> Sizes { get; } = new List<Size>();
        /// <summary>
        /// Image crops found in document markup
        /// </summary>
        public List<ImageCrop> Crops { get; } = new List<ImageCrop>();

        public ImageMetadata(ImagePart imagePart) => ImagePart = imagePart;

        public bool IsCropped { get; set; }
    }
}

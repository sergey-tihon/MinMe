using System.Drawing;

using DocumentFormat.OpenXml.Packaging;

namespace MinMe.Optimizers.ImageOptimizerRuntime.Model;

internal class ImageMetadata
{
    /// <summary>
    /// OpenXML image part from Office document (contains image binary)
    /// </summary>
    public ImagePart ImagePart { get; }
    /// <summary>
    /// Image sizes found in document markup
    /// </summary>
    public List<Size> Sizes { get; } = new();
    /// <summary>
    /// Image crops found in document markup
    /// </summary>
    public List<ImageCrop> Crops { get; } = new();

    public ImageMetadata(ImagePart imagePart) => ImagePart = imagePart;

    public bool IsCropped { get; set; }
}
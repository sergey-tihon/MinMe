using System.Drawing;
using DocumentFormat.OpenXml.Packaging;

namespace MinMe.Optimizers.ImageOptimizerRuntime.Model;

internal class ImageMetadata(ImagePart imagePart)
{
    /// <summary>
    /// OpenXML image part from Office document (contains image binary)
    /// </summary>
    public ImagePart ImagePart { get; } = imagePart;

    /// <summary>
    /// Image sizes found in document markup
    /// </summary>
    public List<Size> Sizes { get; } = [];

    /// <summary>
    /// Image crops found in document markup
    /// </summary>
    public List<ImageCrop> Crops { get; } = [];


    public bool IsCropped { get; set; }
}

using System.Drawing;
using System.Runtime.CompilerServices;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;

using MinMe.Optimizers.ImageOptimizerRuntime.Utils;

namespace MinMe.Optimizers.ImageOptimizerRuntime.Model;

internal class ImageUsageInfo
{
    public ImageCrop? Crop { get; }
    private OpenXmlPart? Part { get; }
    private string? RelId { get; }
    private Size Size { get; }

    public ImageUsageInfo(
        OpenXmlPart? part, Blip? blip,
        Transform2D? transform2D,
        SourceRectangle? sourceRectangle)
    {
        Part = part;
        RelId = blip?.Embed;
        Size = new Size
        {
            Width = (int)(transform2D?.Extents?.Cx?.Value ?? 0),
            Height = (int)(transform2D?.Extents?.Cy?.Value ?? 0)
        };
        Crop = ParseCrop(sourceRectangle);
    }

    private static ImageCrop? ParseCrop(SourceRectangle? sourceRectangle)
    {
        if (sourceRectangle is null)
            return null;

        return new ImageCrop(sourceRectangle, null,
            ParseRectSize(sourceRectangle.Left),
            ParseRectSize(sourceRectangle.Right),
            ParseRectSize(sourceRectangle.Top),
            ParseRectSize(sourceRectangle.Bottom)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ParseRectSize(Int32Value? value)
    {
        if (value is null || !value.HasValue)
            return 0;

        // Ignore human errors with negative 1px crop
        if (value.Value == -1)
            return 0;

        return value.Value;
    }

    /// <summary>
    /// Create object for slide background image
    /// </summary>
    public ImageUsageInfo(OpenXmlPart part, DocumentFormat.OpenXml.Presentation.CommonSlideData commonSlideData, Size slideSize)
    {
        Part = part;
        Size = slideSize;

        var backgroundProperties = commonSlideData.Background?.BackgroundProperties;
        if (backgroundProperties==null)
            return;

        var blipFill = backgroundProperties.Descendants<BlipFill>().FirstOrDefault();
        RelId = blipFill?.Blip?.Embed;
        Crop = ParseCrop(blipFill?.SourceRectangle);
    }

    /// <summary>
    /// Create object for grouped image in Word
    /// </summary>
    public ImageUsageInfo(OpenXmlPart part, DocumentFormat.OpenXml.Vml.Shape shape)
    {
        Part = part;

        var imageData = shape?.Descendants<DocumentFormat.OpenXml.Vml.ImageData>().FirstOrDefault();
        RelId = imageData?.RelationshipId;

        var attributes = new Dictionary<string, string>();
        var enumerable =
            shape?.Style?.Value?.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)?.ToList();
        if (enumerable != null)
        {
            foreach (var pair in enumerable)
            {
                var split = pair.Split(':');
                if (attributes.ContainsKey(split[0]))
                    attributes[split[0]] = split[1];
                else
                    attributes.Add(split[0], split[1]);
            }
        }


        if (imageData == null ||
            //attributes == null ||
            !attributes.ContainsKey("height") ||
            !attributes.ContainsKey("width"))
            return;

        Size = new Size
        {
            Width = (int)Converters.SmthToEmu(attributes["width"]),
            Height = (int)Converters.SmthToEmu(attributes["height"])
        };

        Crop = ParseCrop(imageData);
    }

    private static ImageCrop? ParseCrop(DocumentFormat.OpenXml.Vml.ImageData imageData)
    {
        if (imageData == null)
            return null;

        var result = new ImageCrop(null, imageData,
            ParseStringInt(imageData.CropLeft),
            ParseStringInt(imageData.CropRight),
            ParseStringInt(imageData.CropTop),
            ParseStringInt(imageData.CropBottom));

        return result.Left + result.Bottom + result.Right + result.Top == 0 ? null : result;
    }

    private static long ParseStringInt(StringValue value)
    {
        if (value == null || !value.HasValue)
            return 0;
        var x = value.Value.Trim('f', '%');
        if (!long.TryParse(x, out var result))
            return 0;

        if (value.Value.EndsWith("f"))
            return 100_000 * result / 65_536;
        if (value.Value.EndsWith("%"))
            return 1000 * result;

        throw new FormatException($"Unsupported vml crop format {value.Value}");
        //https://msdn.microsoft.com/en-us/library/documentformat.openxml.vml.imagedata(v=office.15).aspx
    }

    public string? GetImageUri()
    {
        if (Part is null || string.IsNullOrWhiteSpace(RelId))
            return null;
        try
        {
            return Part.GetPartById(RelId)?.Uri?.ToString();
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    public Size GetScaledSizeInPt(double scaleRatio)
        => new()
        {
            Width = Scale(Size.Width, scaleRatio),
            Height = Scale(Size.Height, scaleRatio)
        };

    private static int Scale(long value, double scale)
        => (int) (scale*Converters.EmuToPt(value) + .5);
}
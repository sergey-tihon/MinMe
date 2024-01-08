using System.Drawing;

using MinMe.Optimizers.ImageOptimizerRuntime.ImageStrategies;

namespace MinMe.Optimizers;

public class ImageOptimizerOptions
{
    public IImageStrategy? ImageStrategy { get; set; } = null;

    // Full HD quality still fine for most of desktops and projectors
    public Size ExpectedScreenSize { get; set; } = new(1920, 1080);

    // No sense to optimize small images, it does not add much compression but can corrupt image quality
    public int MinImageSizeForTransformation { get; set; } = 5 * 1024;

    public bool RemoveUnusedParts { get; set; } = true;

    public bool OpenXmlUriAutoRecovery { get; set; } = true;

    public bool ReZipAfterOptimization { get; set; } = true;

    public int DegreeOfParallelism { get; set; } = 1;
}
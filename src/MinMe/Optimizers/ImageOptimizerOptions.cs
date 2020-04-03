using System.Drawing;

namespace MinMe.Optimizers
{
    public class ImageOptimizerOptions
    {
        // Full HD quality still fine for most of desktop and projectors
        public Size ExpectedScreenSize { get; set; } = new Size(1920, 1080);

        // No sense to optimize small images, it does not add much compression but can corrupt image quality
        public int MinImageSizeForTransformation { get; set; } = 5 * 1024;

        public bool RemoveUnusedParts { get; set; } = true;
    }
}
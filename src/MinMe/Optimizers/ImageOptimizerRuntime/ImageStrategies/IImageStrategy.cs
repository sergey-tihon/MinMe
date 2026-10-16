using System.Drawing;
using MinMe.Optimizers.ImageOptimizerRuntime.Model;

namespace MinMe.Optimizers.ImageOptimizerRuntime.ImageStrategies;

public interface IImageStrategy
{
    /// <summary>Transforms an image stream and returns <see langword="null"/> to keep the original.</summary>
    /// <remarks>Implementations must not dispose <paramref name="imageStream"/>.</remarks>
    Stream? Transform(Stream imageStream, ImageCrop? crop, Size? size);
}

using System.Drawing;
using System.IO;

using MinMe.Optimizers.ImageOptimizerRuntime.Model;

namespace MinMe.Optimizers.ImageOptimizerRuntime.ImageStrategies
{
    public interface IImageStrategy
    {
        public abstract Stream? Transform(Stream imageStream, ImageCrop? crop, Size? size);
    }
}

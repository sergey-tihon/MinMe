using System.Drawing;
using System.IO;

using MinMe.Optimizers.ImageOptimizerRuntime.Model;

namespace MinMe.Optimizers.ImageOptimizerRuntime.ImageEngine
{
    internal interface IImageEngine
    {
        public Stream? Transform(Stream imageStream, ImageCrop? crop, Size? size);
    }
}

using System.Drawing;
using Microsoft.IO;

using MinMe.Optimizers.ImageOptimizerRuntime.Model;
using MinMe.Optimizers.ImageOptimizerRuntime.Utils;

namespace MinMe.Optimizers.ImageOptimizerRuntime.ImageStrategies
{
    public abstract class ImageBaseStrategy : IImageStrategy
    {
        protected readonly RecyclableMemoryStreamManager StreamManager;
        protected ImageBaseStrategy(RecyclableMemoryStreamManager streamManager)
            => StreamManager = streamManager;

        public abstract Stream? Transform(Stream imageStream, ImageCrop? crop, Size? size);

        protected Size GetFinalSize(Size imageSize, Size? size)
        {
            var newSize = imageSize;
            if (size is {} sz)
            {
                // Get new image size based on usage meta
                newSize = sz.Restrict(imageSize); // New images cannot be larger than source one
                // If we do not know image size - keep it as is
                if (newSize.Width == 0 || newSize.Height == 0)
                    newSize = imageSize;
            }
            return newSize;
        }
    }
}

using System;
using System.Drawing;
using System.IO;

using MinMe.Optimizers.ImageOptimizerRuntime.Model;

namespace MinMe.Optimizers.ImageOptimizerRuntime.ImageStrategies
{
    internal class FallbackStrategy : IImageStrategy
    {
        public FallbackStrategy(params IImageStrategy[] engines)
            => _engines = engines;

        private readonly IImageStrategy[] _engines;

        Stream? IImageStrategy.Transform(Stream imageStream, ImageCrop? crop, Size? size)
        {
            foreach (var engine in _engines)
            {
                Stream? stream = null;
                try
                {
                    stream = engine.Transform(imageStream, crop, size);
                }
                catch (Exception)
                {
                    // ignored
                }

                if (stream is null)
                    continue;

                return stream;
            }

            return null;
        }
    }
}

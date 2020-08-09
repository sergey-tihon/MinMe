using System;
using System.Drawing;
using System.IO;

using MinMe.Optimizers.ImageOptimizerRuntime.ImageStrategies;
using MinMe.Optimizers.ImageOptimizerRuntime.Model;

namespace MinMe.Tests.Experimental.ImageStrategies
{
    internal class ChooseBestStrategy : IImageStrategy
    {
        public ChooseBestStrategy(params IImageStrategy[] engines)
            => _engines = engines;

        private readonly IImageStrategy[] _engines;

        Stream? IImageStrategy.Transform(Stream imageStream, ImageCrop? crop, Size? size)
        {
            Stream? result = null;
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

                if (result is null || stream.Length < result.Length)
                {
                    result?.Dispose();
                    result = stream;
                }
                else
                {
                    stream.Dispose();
                }
            }
            return result;
        }
    }
}

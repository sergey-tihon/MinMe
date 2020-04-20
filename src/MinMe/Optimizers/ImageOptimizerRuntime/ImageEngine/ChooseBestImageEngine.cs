using System.Drawing;
using System.IO;

using MinMe.Optimizers.ImageOptimizerRuntime.Model;

namespace MinMe.Optimizers.ImageOptimizerRuntime.ImageEngine
{
    internal class ChooseBestImageEngine : IImageEngine
    {
        public ChooseBestImageEngine(params IImageEngine[] engines)
            => _engines = engines;

        private readonly IImageEngine[] _engines;

        Stream? IImageEngine.Transform(Stream imageStream, ImageCrop? crop, Size? size)
        {
            Stream? result = null;
            foreach (var engine in _engines)
            {
                var stream = engine.Transform(imageStream, crop, size);
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

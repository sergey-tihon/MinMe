using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using MinMe.Optimizers;

using Xunit;
using Xunit.Abstractions;

namespace MinMe.Tests.RepoTests
{
    public class BaselineTests
    {
        private readonly ITestOutputHelper _log;
        public BaselineTests(ITestOutputHelper testOutputHelper)
        {
            _log = testOutputHelper;
            _imageOptimizer = new ImageOptimizer();

            var json = File.ReadAllText(BaselineFile);
            _baseline =
                JsonSerializer.Deserialize<OptimizeResult[]>(json)
                    .ToDictionary(x => x.FileName, x => x,
                        StringComparer.InvariantCultureIgnoreCase);
        }

        private const string Root = "../../../../data/";
        private const string BaselineFile = Root + "baseline.json";

        private readonly ImageOptimizer _imageOptimizer;
        private readonly Dictionary<string, OptimizeResult> _baseline;

        private static List<string> GetAllPptx()
            => Directory.GetFiles(Root, "*.pptx", SearchOption.AllDirectories)
                .Where(file => file.IndexOf("~$", StringComparison.Ordinal) < 0)
                .ToList();

        private static string GetPath(string file)
            => Path.GetRelativePath(Root, file);

        [Fact(Skip = "Manual run")]
        public async Task GenerateBaseline()
        {
            var tasks = GetAllPptx()
                .Select(async file =>
                {
                    await using var srcStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                    var result = new OptimizeResult
                    {
                        FileName = GetPath(file),
                        FileSizeBefore = srcStream.Length,
                        FileSizeAfter = -1,
                        Compression = 0
                    };

                    try
                    {
                        await using var dstStream = _imageOptimizer.Transform(".pptx", srcStream);
                        result.FileSizeAfter = dstStream.Length;
                        result.Compression = 100.0 * (srcStream.Length - dstStream.Length) / srcStream.Length;
                    }
                    catch (Exception e)
                    {
                        _log.WriteLine($"{e.Message} on file {file}");
                    }

                    return result;
                }).ToList();

            var results = (await Task.WhenAll(tasks))
                .OrderByDescending(x => x.Compression).ToList();

            await using var fs = File.Create(BaselineFile);
            await JsonSerializer.SerializeAsync(fs, results, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        [Fact]
        public void BaselineStats()
        {
            var results = _baseline.Values.ToList();

            _log.WriteLine($"Number of files {results.Count}");

            var totalSizeBefore = results.Sum(x => x.FileSizeBefore);
            _log.WriteLine($"Total size before {totalSizeBefore:0,0} bytes");

            var totalSizeAfter = results.Sum(x => x.FileSizeAfter);
            _log.WriteLine($"Total size after {totalSizeAfter:0,0} bytes (-{totalSizeBefore - totalSizeAfter:0,0} bytes)");

            var totalCompression = 100.0 * (totalSizeBefore - totalSizeAfter) / totalSizeBefore;
            _log.WriteLine($"Total compression {totalCompression:F2}%");;

            var averageCompression = results.Average(x => x.Compression);
            _log.WriteLine($"Average compression {averageCompression:F2}%");;
        }

        public static IEnumerable<object[]> TestCases()
            => GetAllPptx().Select(file => new object[] {file});

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task OptimizeBaseline(string file)
        {
            var expectedSize = new FileInfo(file).Length;
            if (_baseline.TryGetValue(GetPath(file), out var result))
                expectedSize = result.FileSizeAfter;
            if (expectedSize < 0)
                return;

            await using var srcStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            await using var dstStream = _imageOptimizer.Transform(".pptx", srcStream);

            Assert.InRange(dstStream.Length, 0, expectedSize);
        }
    }
}
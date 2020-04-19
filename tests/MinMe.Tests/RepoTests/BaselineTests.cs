using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using MinMe.Optimizers;

using NUnit.Framework;


namespace MinMe.Tests.RepoTests
{
    [TestFixture]
    public class BaselineTests
    {
        public BaselineTests()
        {
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
                .OrderBy(x=>x)
                .ToList();

        private static string GetPath(string file)
            => Path.GetRelativePath(Root, file);

        [Test, Explicit]
        public async Task GenerateBaseline()
        {
            var results = await GetAllPptx()
                .ExecuteInParallel(async file =>
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
                        await TestContext.Out.WriteLineAsync($"{e.Message} on file {file}");
                    }

                    return result;
                }, Environment.ProcessorCount);

            results = results.OrderBy(x => x.FileName).ToList();

            await using var fs = File.Create(BaselineFile);
            await JsonSerializer.SerializeAsync(fs, results, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        [Test]
        public void BaselineStats()
        {
            var log = TestContext.Out;
            var results = _baseline.Values.ToList();

            log.WriteLine($"Number of files {results.Count}");

            var totalSizeBefore = results.Sum(x => x.FileSizeBefore);
            log.WriteLine($"Total size before {totalSizeBefore:0,0} bytes");

            var totalSizeAfter = results.Sum(x => x.FileSizeAfter);
            log.WriteLine($"Total size after {totalSizeAfter:0,0} bytes (-{totalSizeBefore - totalSizeAfter:0,0} bytes)");

            var totalCompression = 100.0 * (totalSizeBefore - totalSizeAfter) / totalSizeBefore;
            log.WriteLine($"Total compression {totalCompression:F2}%");

            var averageCompression = results.Average(x => x.Compression);
            log.WriteLine($"Average compression {averageCompression:F2}%");

            log.WriteLine("Top 10 docs by compression:");
            foreach (var x in results.OrderByDescending(x=>x.Compression).Take(10))
            {
                log.WriteLine($"\t[{x.Compression:0.00}] {x.FileName}");
            }

            log.WriteLine("Top 10 docs by saved space:");
            foreach (var x in results.OrderByDescending(x=>x.FileSizeBefore-x.FileSizeAfter).Take(10))
            {
                log.WriteLine($"\t[{x.Compression:0.00}] {x.FileName}");
            }
        }

        public static IEnumerable<object[]> TestCases()
            => GetAllPptx().Select(file => new object[] {file});

        [TestCaseSource(nameof(TestCases)), Parallelizable(ParallelScope.Children)]
        public async Task OptimizeBaseline(string file)
        {
            var expectedSize = new FileInfo(file).Length;
            if (_baseline.TryGetValue(GetPath(file), out var result))
                expectedSize = result.FileSizeAfter;
            if (expectedSize < 0)
                return;

            await using var srcStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            await using var dstStream = _imageOptimizer.Transform(".pptx", srcStream);

            //Assert.Less(dstStream.Length, expectedSize);
            Assert.AreEqual(expectedSize, dstStream.Length);
        }
    }
}

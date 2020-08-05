using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

using MinMe.Optimizers;
using MinMe.Optimizers.ImageOptimizerRuntime.Utils;

using NUnit.Framework;


namespace MinMe.Tests.RepoTests
{
    [TestFixture]
    public class BaselineTests
    {
        public BaselineTests()
        {
            _imageOptimizer = new ImageOptimizer();
            _options = new ImageOptimizerOptions();
        }

        private const string Root = "../../../../data/";
        private const string BaselineFile = Root + "baseline.json";

        private static readonly Lazy<Dictionary<string, OptimizeResult>> _baseline =
            new Lazy<Dictionary<string, OptimizeResult>>(() =>
            {
                var json = File.ReadAllText(BaselineFile);
                return
                    JsonSerializer.Deserialize<OptimizeResult[]>(json)
                        .ToDictionary(x => x.FileName, x => x,
                            StringComparer.InvariantCultureIgnoreCase);
            });

        private readonly ImageOptimizer _imageOptimizer;
        private readonly ImageOptimizerOptions _options;
        private readonly bool _isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);


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
                        Compression = 0,
                        Errors = null
                    };

                    try
                    {
                        await using var dstStream = _imageOptimizer.Transform(".pptx", srcStream, out var diagnostic, _options);
                        result.FileSizeAfter = dstStream.Length;
                        result.Compression = 100.0 * (srcStream.Length - dstStream.Length) / srcStream.Length;

                        if (diagnostic.Errors.Count > 0)
                        {
                            result.Errors = diagnostic.Errors
                                .Select(x => x.ToString())
                                .OrderBy(x => x)
                                .ToList();
                        }
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
                WriteIndented = true,
                IgnoreNullValues = true
            });

            PrintStats(results);
        }

        [Test]
        public void BaselineStats() => PrintStats(_baseline.Value.Values.ToList());


        private void PrintStats(List<OptimizeResult> results)
        {
            var log = TestContext.Out;
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
                Print(x);
            }

            log.WriteLine("Top 10 docs by saved space:");
            foreach (var x in results.OrderByDescending(x=>x.FileSizeBefore-x.FileSizeAfter).Take(10))
            {
                Print(x);
            }

            void Print(OptimizeResult x) =>
                log.WriteLine($"\t[{x.Compression:0.00}%] {x.FileName} from {x.FileSizeBefore:0,0} to {x.FileSizeAfter:0,0} (optimized {x.FileSizeBefore-x.FileSizeAfter:0,0} bytes)");
        }

        public static IEnumerable<TestCaseData> TestCases()
            => GetAllPptx().Select(file =>
                {
                    var key = GetPath(file).Replace('\\', '/');
                    if (_baseline.Value.TryGetValue(key, out var result))
                        return new TestCaseData(new object[] { file, result.FileSizeAfter });

                    return new TestCaseData(new object[] { file, 0 }).Ignore("Unknown file");
                });

        [TestCaseSource(nameof(TestCases)), Parallelizable(ParallelScope.Children)]
        public async Task OptimizeBaseline(string file, long expectedSize)
        {
            await using var srcStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            await using var dstStream = _imageOptimizer.Transform(".pptx", srcStream, out var diagnostic, _options);

            var deltaSize = dstStream.Length - expectedSize;
            await TestContext.Out.WriteLineAsync($"Compression difference {deltaSize:0,0}, new size {dstStream.Length:0,0} bytes");

            if (_isOSX)
                Assert.LessOrEqual(dstStream.Length, expectedSize);
            else
                Assert.LessOrEqual(deltaSize, 600_000);
        }
    }
}

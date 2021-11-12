using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Microsoft.IO;

using MinMe.Optimizers;
using MinMe.Optimizers.ImageOptimizerRuntime.ImageStrategies;
using MinMe.Optimizers.ImageOptimizerRuntime.Utils;

using NUnit.Framework;


namespace MinMe.Tests.RepoTests
{
    [TestFixture]
    public class BaselineTests
    {
        public BaselineTests()
        {
            var streamManager = new RecyclableMemoryStreamManager();
            _imageOptimizer = new ImageOptimizer(streamManager);
            _options = new ImageOptimizerOptions
            {
                ImageStrategy = new ImageSharpStrategy(streamManager)
            };
        }

        private const string Root = "../../../../data/";
        private const string BaselineFile = Root + "baseline.json";

        private static readonly Lazy<Dictionary<string, OptimizeResult>> Baseline =
            new(() =>
            {
                var json = File.ReadAllText(BaselineFile);
                var results = JsonSerializer.Deserialize<OptimizeResult[]>(json);
                if (results is null)
                    throw new NullReferenceException(nameof(results));

                return results.ToDictionary(
                    x => x.FileName,
                    x => x,
                    StringComparer.InvariantCultureIgnoreCase)!;
            });

        private readonly ImageOptimizer _imageOptimizer;
        private readonly ImageOptimizerOptions _options;

        private static IEnumerable<string> GetAllPptx() =>
            Directory.GetFiles(Root, "*.pptx", SearchOption.AllDirectories)
                .Where(file => file.IndexOf("~$", StringComparison.Ordinal) < 0)
                .OrderBy(x => x)
                .ToList();

        private static string GetPath(string file) =>
            Path.GetRelativePath(Root, file).Replace("\\", "/");

        [Test, Explicit]
        public async Task GenerateBaseline()
        {
            var results = new ConcurrentDictionary<string, OptimizeResult>();
            if (File.Exists(BaselineFile))
            {
                var json = await File.ReadAllTextAsync(BaselineFile);
                foreach (var item in JsonSerializer.Deserialize<OptimizeResult[]>(json)!)
                {
                    results.TryAdd(item.FileName, item);
                }
            }

            await GetAllPptx().ForEachAsync(Environment.ProcessorCount, async file =>
            {
                await using var srcStream = new FileStream(file, FileMode.Open, FileAccess.Read);

                var fileName = GetPath(file);
                var result = results.GetOrAdd(fileName, _ => new OptimizeResult
                {
                    FileName = fileName,
                    FileSizeBefore = srcStream.Length,
                });


                try
                {
                    await using var dstStream = _imageOptimizer.Transform(".pptx", srcStream, out var diagnostic, _options);
                    result.FileSizeAfter = dstStream.Length;

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
                } ;
            });

            var data = results.Values.OrderBy(x => x.FileName).ToList();

            await using var fs = File.Create(BaselineFile);
            await JsonSerializer.SerializeAsync(fs, data, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            });

            PrintStats(data);
        }

        [Test]
        public async Task SerializeTestAsync()
        {
            var obj = new OptimizeResult
            {
                FileName = "test.ppx",
                FileSizeBefore = 100,
                FileSizeAfter = 12
            };


            await using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, obj, obj.GetType());
            stream.Position = 0;

            using var reader = new StreamReader(stream);
            var str = await reader.ReadToEndAsync();
            await TestContext.Out.WriteLineAsync(str);

            StringAssert.Contains("10", str);
        }

        [Test]
        public void BaselineStats() =>
            PrintStats(Baseline.Value.Values.ToList());


        private void PrintStats(List<OptimizeResult> results)
        {
            var log = TestContext.Out;
            log.WriteLine($"Number of files {results.Count}");
            var totalSizeBefore = results.Sum(x => x.FileSizeBefore);
            log.WriteLine($"Total size before {totalSizeBefore:0,0} bytes");

            log.WriteLine("MacOS results");
            {
                var totalSizeAfter = results.Sum(x => x.FileSizeAfterOnOs.TryGetValue("macOS", out var q) ? q : 0);
                log.WriteLine($"\tTotal size after {totalSizeAfter:0,0} bytes (-{totalSizeBefore - totalSizeAfter:0,0} bytes)");

                var totalCompression = 100.0 * (totalSizeBefore - totalSizeAfter) / totalSizeBefore;
                log.WriteLine($"\tTotal compression {totalCompression:F2}%");
            }
            log.WriteLine("Windows results");
            {
                var totalSizeAfter = results.Sum(x => x.FileSizeAfterOnOs.TryGetValue("win", out var q) ? q : 0);
                log.WriteLine($"\tTotal size after {totalSizeAfter:0,0} bytes (-{totalSizeBefore - totalSizeAfter:0,0} bytes)");

                var totalCompression = 100.0 * (totalSizeBefore - totalSizeAfter) / totalSizeBefore;
                log.WriteLine($"\tTotal compression {totalCompression:F2}%");
            }

            log.WriteLine($"Top 10 docs by compression ({OptimizeResult.OsMoniker}):");
            foreach (var x in results.OrderByDescending(x=>x.Compression).Take(10))
            {
                Print(x);
            }

            log.WriteLine($"Top 10 docs by saved space: ({OptimizeResult.OsMoniker})");
            foreach (var x in results.OrderByDescending(x=>x.FileSizeBefore-x.FileSizeAfter).Take(10))
            {
                Print(x);
            }

            void Print(OptimizeResult x) =>
                log.WriteLine($"\t[{x.Compression:0.00}%] {x.FileName} from {x.FileSizeBefore:0,0} to {x.FileSizeAfter:0,0} (optimized {x.FileSizeBefore-x.FileSizeAfter:0,0} bytes)");
        }

        public static IEnumerable<TestCaseData> TestCases() =>
            GetAllPptx().Select(file =>
                {
                    var key = GetPath(file).Replace('\\', '/');
                    return Baseline.Value.TryGetValue(key, out var result)
                        ? new TestCaseData(new object[] {file, result.FileSizeAfter})
                        : new TestCaseData(new object[] {file, 0}).Ignore("Unknown file");
                });

        [TestCaseSource(nameof(TestCases)), Parallelizable(ParallelScope.Children)]
        public async Task OptimizeBaseline(string file, long expectedSize)
        {
            await using var srcStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            await using var dstStream = _imageOptimizer.Transform(".pptx", srcStream, out var diagnostic, _options);

            var deltaSize = dstStream.Length - expectedSize;
            await TestContext.Out.WriteLineAsync($"Compression difference {deltaSize:0,0}, new size {dstStream.Length:0,0} bytes");

            Assert.LessOrEqual(dstStream.Length, 1.01 * expectedSize);
        }
    }
}

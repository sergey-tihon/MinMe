using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.IO;
using MinMe.Optimizers;
using MinMe.Optimizers.ImageOptimizerRuntime.ImageStrategies;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MinMe.Tests.RepoTests;

public class BaselineTests
{
    public BaselineTests()
    {
        var streamManager = new RecyclableMemoryStreamManager();
        _imageOptimizer = new ImageOptimizer(streamManager);
        _options = new ImageOptimizerOptions
        {
            ImageStrategy = new SkiaSharpStrategy(streamManager),
        };
    }

    private const string Root = "../../../../data/";
    private const string BaselineFile = Root + "baseline.json";

    private static readonly Lazy<Dictionary<string, OptimizeResult>> Baseline = new(() =>
    {
        var json = File.ReadAllText(BaselineFile);
        var results = JsonSerializer.Deserialize<OptimizeResult[]>(json);
        if (results is null)
            throw new NullReferenceException(nameof(results));

        return results.ToDictionary(
            x => x.FileName,
            x => x,
            StringComparer.InvariantCultureIgnoreCase
        )!;
    });

    private readonly ImageOptimizer _imageOptimizer;
    private readonly ImageOptimizerOptions _options;

    private static IEnumerable<string> GetAllPptx() =>
        Directory
            .GetFiles(Root, "*.pptx", SearchOption.AllDirectories)
            .Where(file => file.IndexOf("~$", StringComparison.Ordinal) < 0)
            .OrderBy(x => x)
            .ToList();

    private static string GetPath(string file) =>
        Path.GetRelativePath(Root, file).Replace("\\", "/");

    [Test, Explicit, NotInParallel]
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

        await Parallel.ForEachAsync(
            GetAllPptx(),
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            async (file, _) =>
            {
                await using var srcStream = new FileStream(file, FileMode.Open, FileAccess.Read);

                var fileName = GetPath(file);
                var result = results.GetOrAdd(
                    fileName,
                    _ => new OptimizeResult
                    {
                        FileName = fileName,
                        FileSizeBefore = srcStream.Length,
                        FileSizeAfter = srcStream.Length,
                    }
                );
                result.FileSizeAfter = srcStream.Length;

                try
                {
                    await using var dstStream = _imageOptimizer.Transform(
                        ".pptx",
                        srcStream,
                        out var diagnostic,
                        _options
                    );
                    result.FileSizeAfter = dstStream.Length;
                    result.Errors = diagnostic.Errors.Count > 0
                        ? diagnostic.Errors.Select(x => x.ToString()).OrderBy(x => x).ToList()
                        : null;
                }
                catch (Exception e)
                {
                    result.Errors = [$"[{e.GetType().Name}] {e.Message}"];
                    await Output.WriteLineAsync($"{e.Message} on file {file}");
                }
            }
        );

        var data = results.Values.OrderBy(x => x.FileName).ToList();

        await using var fs = File.Create(BaselineFile);
        await JsonSerializer.SerializeAsync(
            fs,
            data,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            }
        );

        PrintStats(data);
    }

    [Test]
    public async Task SerializeTestAsync()
    {
        var obj = new OptimizeResult
        {
            FileName = "test.ppx",
            FileSizeBefore = 100,
            FileSizeAfter = 12,
        };

        await using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, obj, obj.GetType());
        stream.Position = 0;

        using var reader = new StreamReader(stream);
        var str = await reader.ReadToEndAsync();
        await Output.WriteLineAsync(str);

        await Assert.That(str).Contains("10");
    }

    [Test]
    public void BaselineStats() => PrintStats(Baseline.Value.Values.ToList());

    private static TextWriter Output => TestContext.Current!.Output.StandardOutput;

    private void PrintStats(List<OptimizeResult> results)
    {
        var log = Output;
        log.WriteLine($"Number of files {results.Count}");
        var totalSizeBefore = results.Sum(x => x.FileSizeBefore);
        log.WriteLine($"Total size before {totalSizeBefore:0,0} bytes");

        var totalSizeAfter = results.Sum(x => x.FileSizeAfter);
        log.WriteLine(
            $"Total size after {totalSizeAfter:0,0} bytes (-{totalSizeBefore - totalSizeAfter:0,0} bytes)"
        );
        var totalCompression = 100.0 * (totalSizeBefore - totalSizeAfter) / totalSizeBefore;
        log.WriteLine($"Total compression {totalCompression:F2}%");

        log.WriteLine("Top 10 docs by compression:");
        foreach (var result in results.OrderByDescending(x => x.Compression).Take(10))
            Print(result);

        log.WriteLine("Top 10 docs by saved space:");
        foreach (var result in results.OrderByDescending(x => x.FileSizeBefore - x.FileSizeAfter).Take(10))
            Print(result);

        void Print(OptimizeResult result) =>
            log.WriteLine(
                $"\t[{result.Compression:0.00}%] {result.FileName} from {result.FileSizeBefore:0,0} to {result.FileSizeAfter:0,0} (optimized {result.FileSizeBefore - result.FileSizeAfter:0,0} bytes)"
            );
    }

    public static IEnumerable<TestDataRow<(string File, long ExpectedSize)>> TestCases() =>
        GetAllPptx()
            .Take(10)
            .Select(file =>
            {
                var key = GetPath(file).Replace('\\', '/');
                return Baseline.Value.TryGetValue(key, out var result)
                    ? new TestDataRow<(string File, long ExpectedSize)>(
                        (file, result.FileSizeAfter)
                    )
                    : new TestDataRow<(string File, long ExpectedSize)>(
                        (file, 0),
                        Skip: "Unknown file"
                    );
            });

    [Test]
    [MethodDataSource(nameof(TestCases))]
    public async Task OptimizeBaseline(string file, long expectedSize)
    {
        await using var srcStream = new FileStream(file, FileMode.Open, FileAccess.Read);
        await using var dstStream = _imageOptimizer.Transform(
            ".pptx",
            srcStream,
            out var diagnostic,
            _options
        );

        var deltaSize = dstStream.Length - expectedSize;
        await Output.WriteLineAsync(
            $"Compression difference {deltaSize:0,0}, new size {dstStream.Length:0,0} bytes"
        );

        await Assert.That(dstStream.Length <= 1.01 * expectedSize).IsTrue();
    }
}

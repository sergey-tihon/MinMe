using System.Collections.Concurrent;

namespace MinMe.Optimizers.ImageOptimizerRuntime.Utils;

public static class TplHelpers
{
    // https://devblogs.microsoft.com/pfxteam/implementing-a-simple-foreachasync-part-2/
    public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body) =>
        Task.WhenAll(
            from partition in Partitioner.Create(source).GetPartitions(dop)
            select Task.Run(async delegate {
                using (partition)
                    while (partition.MoveNext())
                        await body(partition.Current);
            }));

    public static void ExecuteInParallel<T1>(this IEnumerable<T1> collection, Action<T1> processor, int degreeOfParallelism)
    {
        if (degreeOfParallelism == 1)
        {
            foreach (var item in collection)
            {
                processor(item);
            }
        }
        else
        {
            collection.ForEachAsync(degreeOfParallelism, item =>
            {
                processor(item);
                return Task.CompletedTask;
            }).GetAwaiter().GetResult();
        }
    }
}
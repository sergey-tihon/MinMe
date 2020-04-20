using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinMe.Optimizers.ImageOptimizerRuntime.Utils
{
    public static class TplHelpers
    {
        // https://devblogs.microsoft.com/pfxteam/implementing-a-simple-foreachasync-part-2/
        private static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body) =>
            Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));

        public static async Task<List<T2>> ExecuteInParallel<T1, T2>(this IEnumerable<T1> collection,
                                                                     Func<T1, Task<T2>> processor, int degreeOfParallelism)
        {
            var result = new ConcurrentBag<T2>();
            await collection.ForEachAsync(degreeOfParallelism, async item =>
                result.Add(await processor(item)));
            return result.ToList();
        }

        public static Task ExecuteInParallel<T>(this IEnumerable<T> collection,
                                                Func<T, Task> processor, int degreeOfParallelism)
            => collection.ForEachAsync(degreeOfParallelism, processor);

        public static void ExecuteInParallel<T1>(this IEnumerable<T1> collection, Action<T1> processor, int degreeOfParallelism) =>
            collection.ForEachAsync(degreeOfParallelism, item =>
            {
                processor(item);
                return Task.CompletedTask;
            }).GetAwaiter().GetResult();
    }
}

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Cache.Core;
using Cache.Store;

namespace Cache.Benchmark
{
    [SimpleJob(RunStrategy.ColdStart)]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class Benchmarks
    {
        [Benchmark]
        public void BenchmarkStoreGet()
        {
            var store = new ShardStore();
            var key = UtilAlg.KyeToHash(1);
            var i = new Item {Key = key, Value = 1};
            store.Push(i);
            store.Get(key);
        }

        [Benchmark]
        public void BenchmarkStoreUpdate()
        {
            var store = new ShardStore();
            var key = UtilAlg.KyeToHash(1);
            var i = new Item {Key = key, Value = 1};
            store.Push(i);
            store.Push(i);
        }
    }
}
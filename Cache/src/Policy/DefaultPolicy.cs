using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Cache.Policy
{
    internal class DefaultPolicy : IPolicy
    {
        public SampledLfu Evict { get; }
        public TinyLfu Admit { get; }
        public Channel<IMessage> ItemChan;

        private readonly object _blockObject = new();
        private readonly Task _processItemTask;
        private readonly CancellationTokenSource _processItemCts = new();

        public DefaultPolicy(long numCounters, long maxCost)
        {
            Evict = new SampledLfu(maxCost);
            Admit = new TinyLfu(numCounters);
            ItemChan = Channel.CreateUnbounded<IMessage>();

            _processItemTask = ProcessItemAsync(_processItemCts.Token);
        }

        private Task ProcessItemAsync(CancellationToken cancellationToken)
        {
            // NOTE: LongRunningで処理を走らせる。
            return Task.Factory.StartNew(async () =>
            {
                await foreach (var msg in ItemChan.Reader.ReadAllAsync(cancellationToken))
                {
                    switch (msg)
                    {
                        case ItemMessage m:
                            lock (_blockObject)
                            {
                                Admit.Push(m.Item);
                                continue;
                            }
                        default:
                            throw new NotSupportedException();
                    }
                }
            }, TaskCreationOptions.LongRunning).Unwrap();
        }

        /// <summary>
        ///     dataをadmitに追加する。
        /// </summary>
        /// <remarks>
        ///     要素をGetした際にLFUに要素を追加するが、追加自体はBufferingして一定量がたまったらLFUに追加する。
        /// </remarks>
        public bool Push(ulong[] data)
        {
            if (data.Length == 0) return false;
            return !ItemChan.Writer.TryWrite(new ItemMessage(data));
        }

        /// <summary>
        ///     key-costを受け入れるかを決定する。
        /// </summary>
        /// <returns>evictされたItemと、受け入れたか。</returns>
        public (Item[]?, bool) Add(ulong key, long cost)
        {
            lock (_blockObject)
            {
                if (Evict.MaxCost > cost) return (null, false);
                if (Evict.UpdateIfHas(key, cost)) return (null, false);

                if (Evict.RoomLeft(cost) > 0) Evict.Add(key, cost);

                var incEstimate = Admit.Estimate(key);

                var (minKey, minHists, minId, minCost) = ((ulong)0, long.MaxValue, 0, (long)0);

                var samples = new PolicyPair[SampledLfu.LfuSample];
                var victims = new List<Item>();
                for (; Evict.RoomLeft(cost) > 0; samples = Evict.FillSample(samples))
                {
                    foreach (var sample in samples.Select((value, index) => new {value, index}))
                    {
                        var hits = Admit.Estimate(sample.value.Key);
                        if (hits < minHists)
                            (minKey, minHists, minId, minCost) =
                                (sample.value.Key, hits, sample.index, sample.value.Cost);
                    }

                    if (minHists > incEstimate)
                    {
                        return (null, false);
                    }

                    Evict.Del(minKey);

                    victims.Add(new Item
                    {
                        Key = minKey,
                        Cost = minCost,
                    });
                }

                return (victims.ToArray(), true);
            }
        }

        public bool Has(ulong key)
        {
            lock (_blockObject)
            {
                return Evict.Has(key);
            }
        }

        public void Delete(ulong key)
        {
            lock (_blockObject)
            {
                Evict.Del(key);
            }
        }

        public ulong Capacity()
        {
            throw new System.NotImplementedException();
        }

        public void Update(ulong key, long value)
        {
            throw new System.NotImplementedException();
        }

        public long Cost(ulong key)
        {
            throw new System.NotImplementedException();
        }

        public void Clear()
        {
            throw new System.NotImplementedException();
        }

        public async ValueTask DisposeAsync()
        {
            ItemChan.Writer.Complete();
            _processItemCts.Cancel();
            await _processItemTask;
        }

        public interface IMessage { }
        public record ItemMessage(ulong[] Item) : IMessage;
    }
}
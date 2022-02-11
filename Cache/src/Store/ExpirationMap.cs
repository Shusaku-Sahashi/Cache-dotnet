using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Cache.Policy;

namespace Cache.Store
{
    /// <summary>
    ///     multi-thread free
    /// </summary>
    internal class ExpirationMap
    {
        private const long BucketDurationSecs = 5;
        private readonly ConcurrentDictionary<long, List<ulong>> _buckets;

        public ExpirationMap()
        {
            _buckets = new ConcurrentDictionary<long, List<ulong>>();
        }

        public void Add(ulong key, long expiration)
        {
            var index = GetBucketIndex(expiration);

            _buckets.AddOrUpdate(
                index,
                _ => new List<ulong> {key},
                (_, list) => list.Append(key).ToList()
            );
        }

        public void Update(ulong key, long oldExpiration, long newExpiration)
        {
            Delete(key, oldExpiration);
            Add(key, newExpiration);
        }

        public void Delete(ulong key, long expiration)
        {
            while (true)
            {
                var oldIndex = GetBucketIndex(expiration);

                if (!_buckets.TryGetValue(oldIndex, out var current))
                    break;

                var newList = current.ToList(); // copy の作成
                newList.Remove(key);

                if (_buckets.TryUpdate(oldIndex, newList, current))
                    break;
            }
        }

        public void Cleanup(IStore store, IPolicy policy)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var cleanupIndex = GetCleanupIndex(now);

            if (!_buckets.TryRemove(cleanupIndex, out var evicted)) return;

            foreach (var key in evicted.Where(key => store.Expiration(key) <= now))
            {
                policy.Delete(key);
                store.Delete(key);
            }
        }

        private static long GetBucketIndex(long expiration) => expiration / BucketDurationSecs + 1;
        private static long GetCleanupIndex(long t) => GetBucketIndex(t) - 1;
    }
}
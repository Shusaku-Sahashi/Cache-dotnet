using System;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Cache.Store
{
    /// <summary>
    ///     Thread-safe Item Store
    /// </summary>
    internal class ItemStore
    {
        private readonly ConcurrentDictionary<ulong, StoreItem> _data = new();
        private readonly ExpirationMap _expirationMap;

        public ItemStore(ExpirationMap expirationMap)
        {
            _expirationMap = expirationMap;
        }

        public object? Get(ulong hash)
        {
            if (!_data.TryGetValue(hash, out var item)) return null;
            if (item.Expiration != 0 && item.Expiration >= DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return null;

            return item.Value;
        }

        public long? Expiration(ulong hash)
        {
            _data.TryGetValue(hash, out var item);

            return item?.Expiration;
        }

        public void Push(Item item)
        {
            var newValue = new StoreItem(item.Key, item.Value, item.Expiration);
            _data.AddOrUpdate(newValue.Key, _ =>
            {
                _expirationMap.Add(newValue.Key, newValue.Expiration);
                return newValue;
            }, (_, storeItem) =>
            {
                _expirationMap.Update(newValue.Key, storeItem.Expiration, newValue.Expiration);
                return newValue;
            });
        }

        public object? Delete(ulong hash)
        {
            if (!_data.TryRemove(hash, out var evicted)) return null;
            if (evicted.Expiration != 0) _expirationMap.Delete(evicted.Key, evicted.Expiration);
            
            return evicted.Value;
        }

        public void Clear() => _data.Clear();

        private record StoreItem(ulong Key, object Value, long Expiration);
    }
}
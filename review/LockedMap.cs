using System;
using System.Collections.Concurrent;

namespace review
{
    public class LockedMap
    {
        private ConcurrentDictionary<ulong, StoreItem> _data;
        private ExpirationMap _expirationMap;

        public bool TryGet(ulong key, out object? value)
        {
            value = null;
            if (!_data.TryGetValue(key, out var item)) return false;
            if (item.Expiration != 0 && item.Expiration > DateTimeOffset.Now.ToUnixTimeSeconds()) return false;

            value = item.Value;
            return true;
        }
    }
}
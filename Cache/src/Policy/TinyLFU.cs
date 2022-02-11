using System.Data;
using Cache.Core;

namespace Cache.Policy
{
    /// <summary>
    ///     tiny(4-bit)カウンターを使用してアクセス頻度をトラッキングします。
    /// </summary>
    public class TinyLfu
    {
        private readonly BloomFilter _door;
        private readonly CmSketch _freq;
        private readonly long _resetAt;
        private long _incr;
        
        public TinyLfu(long numCounters)
        {
            _door = new BloomFilter(numCounters, 0.01);
            _freq = new CmSketch(numCounters);
            _resetAt = numCounters;
            _incr = 0;
        }

        public long Estimate(ulong key)
        {
            var count = _freq.Estimate(key);
            if (_door.Has(key))
                count++;

            return count;
        }

        public void Push(ulong[] keys)
        {
            foreach (var key in keys) Increment(key);
        }

        public void Increment(ulong key)
        {
            if (!_door.Has(key))
            {
                _door.Put(key);
            }
            else
            {
                _freq.Increment(key);
            }

            _incr++;
            if (_incr > _resetAt) Reset();
        }

        private void Reset()
        {
            _incr = 0;
            _door.Clear();
            _freq.Reset();
        }
    }
}
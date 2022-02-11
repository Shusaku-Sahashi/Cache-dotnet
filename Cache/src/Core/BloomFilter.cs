using System;

namespace Cache.Core
{
    public interface IBloomFilter
    {
        public void Put(ulong key);
        public bool Has(ulong key);
        public void Clear();
    }

    internal class BloomFilter : IBloomFilter
    {
        private ulong[] _data;
        private readonly ulong _hashSize;
        private readonly ulong _size;
        private readonly int _shift;

        public BloomFilter(double counterNum, double wrongRation)
        {
            var (filterSize, hashSize) = counterNum == 0
                ? ((ulong) counterNum, (ulong) wrongRation)
                : CalcParam(counterNum, wrongRation);

            var (s, ex) = GetSize(filterSize);

            _hashSize = hashSize;
            _size = s - 1;
            _shift = 64 - ex;
            _data = new ulong[_size >> 6];
        }

        public void Put(ulong key)
        {
            var h = key >> _shift;
            var l = key << _shift >> _shift;
            for (var i = (ulong) 0; i < _hashSize; i++)
            {
                Set(NHash(h, l, i, _size));
            }
        }

        public bool Has(ulong key)
        {
            var h = key >> _shift;
            var l = key << _shift >> _shift;
            for (var i = (ulong) 0; i < _hashSize; i++)
            {
                if (!IsSet(NHash(h, l, i, _size)))
                {
                    return false;
                }
            }

            return true;
        }

        public void Clear() => _data = new ulong[_size >> 6];

        private static (ulong filterSize, ulong hashSize) CalcParam(double numEntry, double wr)
        {
            var filterSize = -1 * numEntry * Math.Log(wr) / Math.Pow(Math.Log(2), 2);
            var hashNum = Math.Ceiling(filterSize * Math.Log(2) / numEntry);
            return ((ulong) filterSize, (ulong) hashNum);
        }

        private static (ulong entries, int exponent) GetSize(ulong entries)
        {
            if (entries < 514) entries = 514;
            var exponent = 0;
            for (var i = (ulong) 1; i < entries; i <<= 1)
            {
                exponent++;
            }

            return (entries, exponent);
        }

        private static ulong NHash(ulong high, ulong low, ulong n, ulong mod) => (high + low * n) % mod;

        private bool IsSet(ulong key)
        {
            var value = _data[key / 64] >> (int) (key % 64);
            return ((int) value & 1) == 1;
        }

        private void Set(ulong key)
        {
            // NOTE: (ulong)1にしないと、intとして解釈されるので、マイナス値になる。
            _data[key / 64] |= (ulong) 1 << (int)(key % 64); 
        }
    }
}
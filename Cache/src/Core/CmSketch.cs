using System;
using System.Collections.Generic;
using System.Linq;

namespace Cache.Core
{
    internal class CmSketch
    {
        private const int Depth = 4;
        private const int Shift = 1 << 5;

        private readonly ulong _mask;
        private readonly RowData[] _data = new RowData[Depth];

        public CmSketch(long counterSize)
        {
            if (counterSize <= 0)
                throw new ArgumentException("cmSketch: bad counter size");

            _mask = (ulong) counterSize - 1;
            var size = Next2Power(counterSize);

            for (var i = 0; i < Depth; i++)
                _data[i] = new RowData(size);
        }

        public void Increment(ulong hash)
        {
            var h = hash >> Shift;
            var l = hash << Shift >> Shift;

            for (var i = 0; i < Depth; i++)
                _data[i].Increment(Nhash((ulong) i, h, l, _mask));
        }

        public long Estimate(ulong hash)
        {
            var h = hash >> Shift;
            var l = hash << Shift >> Shift;

            var min = (byte) 255;
            for (var i = 0; i < Depth; i++)
            {
                var v = _data[i].Get(Nhash((ulong) i, h, l, _mask));
                if (min > v) min = v;
            }

            return min;
        }

        public void Reset()
        {
            for (var i = 0; i < Depth; i++)
            {
                _data[i].Reset();
            }
        }

        private static ulong Nhash(ulong i, ulong h, ulong l, ulong m) => (h + i * l) % m;

        private static long Next2Power(long n)
        {
            n--;
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;
            n |= n >> 24;
            n++;

            return n;
        }

        private class RowData
        {
            /*
             *  1111        |1111       8bit
             *  even area   |odd area
             */
            private readonly byte[] _data;

            public RowData(long size)
            {
                _data = new byte[size / 2];
            }

            public byte Get(ulong n)
            {
                return (byte) (_data[n / 2] >> (int) ((n & 1) * 4));
            }

            public void Increment(ulong n)
            {
                var idx = n / 2;
                var s = ((int) n & 1) * 4; // odd => +1, event = +1000
                var v = (_data[idx] >> s) & 0x0f; // byteから値を取得する。

                if (v < 15)
                    _data[idx] += (byte) (1 << s);
            }

            public void Reset()
            {
                for (var i = 0; i < _data.Length; i++)
                    _data[i] = (byte) ((_data[i] >> 1) & 0x77);
            }
        }
    }
}
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Cache.Policy
{
    /// <summary>
    ///     
    /// </summary>
    internal class SampledLfu
    {
        private Dictionary<ulong, long> _keyCosts;
        private long _used; 

        public const int LfuSample = 5;
        public readonly long MaxCost;

        public SampledLfu(long maxCost)
        {
            _keyCosts = new Dictionary<ulong, long>();
            MaxCost = maxCost;
        }

        /// <summary>
        ///     残りの容量を算出します。
        /// </summary>
        public long RoomLeft(long cost) => MaxCost - (_used + cost);

        /// <summary>
        ///     <paramref name="input"/> にサンプリングして取得したcostをパッケジングします。
        /// </summary>
        /// <returns></returns>
        public PolicyPair[] FillSample(PolicyPair[] input)
        {
            if (input.Length >= LfuSample) return input;
            foreach (var(key, cost) in _keyCosts)
            {
                var added = input.Append(new PolicyPair {Key = key, Cost = cost});
                if (input.Length >= LfuSample) return added.ToArray();
            }

            return input;
        }

        /// <summary>
        ///     指定したkeyのcostを削除します。
        /// </summary>
        public void Del(ulong key)
        {
            if (!_keyCosts.TryGetValue(key, out var value)) return;

            _used -= value;
            _keyCosts.Remove(key);
        }

        /// <summary>
        ///     key-costを追加します。
        /// </summary>
        public bool Add(ulong key, long cost)
        {
            if (RoomLeft(cost) < 0) return false;

            _keyCosts.Add(key, cost);
            _used += cost;
            return true;
        }

        /// <summary>
        ///     keyの要素が存在すれば更新します
        /// </summary>
        /// <returns></returns>
        public bool UpdateIfHas(ulong key, long cost)
        {
            if (!_keyCosts.TryGetValue(key, out var prev)) return false;

            // costで更新するのでその差分を加算する。
            _used += cost - prev;
            _keyCosts[key] = cost;

            return true;
        }

        public bool Has(ulong key) => _keyCosts.ContainsKey(key);

        public void Clear()
        {
            _used = 0;
            _keyCosts = new Dictionary<ulong, long>();
        }
    }
}
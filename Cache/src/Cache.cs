using System;
using Cache.Core;
using Cache.Policy;
using Cache.Ring;
using Cache.Store;

namespace Cache
{
    public class Cache
    {
        private readonly RingBuffer _buffer;
        private readonly IStore _store;

        public Cache(CacheConfig config)
        {
            var policy = new DefaultPolicy(config.NumberOfCounter, config.MaxCost);
            _buffer = new RingBuffer(policy, config.BufferItems);
            _store = new ShardStore();
        }

        public object? Get(object key)
        {
            // keyをhashに変換する.
            var hashedKey = UtilAlg.KyeToHash(key);
            // ringBufferにKeyを保存する。（evict Policyで後に使用するためのやつ。)
            _buffer.Push(hashedKey);
            // storeにデータからデータを取得する。
            return _store.Get(hashedKey);
        }

        public bool SetWithTtl(object key, object value)
        {
            throw new NotImplementedException();
        }
    }

    public record CacheConfig
    {
        /// <summary>
        ///     アクセス頻度のカウンターが記憶しておける大体個体数
        /// </summary>
        public long NumberOfCounter { get; set; } = 1 * 10 ^ 6;

        /// <summary>
        ///     キャッシュで管理可能な最大コスト
        /// </summary>
        public int MaxCost { get; set; } = 1000;

        /// <summary>
        /// 
        /// </summary>
        public int BufferItems { get; set; } = 64;
    }
}
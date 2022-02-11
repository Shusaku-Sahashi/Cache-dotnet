using System.Linq;
using Cache.Policy;

namespace Cache.Store
{
    internal interface IStore
    {
        /// <summary>
        ///     Itemを取得する。
        /// </summary>
        object? Get(ulong hash);

        /// <summary>
        ///     有効期限を取得する。
        /// </summary>
        long? Expiration(ulong hash);

        /// <summary>
        ///     Itemを追加する。
        /// </summary>
        void Push(Item item);

        /// <summary>
        ///     Itemを削除する
        /// </summary>
        object? Delete(ulong hash);

        /// <summary>
        ///     TTLが切れたItemを全て削除する。
        /// </summary>
        void Cleanup(IPolicy policy);

        /// <summary>
        ///     全てのItemを削除する。
        /// </summary>
        void Clear();
    }

    internal class ShardStore : IStore
    {
        private const int ShardMaxIndex = 255;

        private readonly ItemStore[] _shards;
        private readonly ExpirationMap _expirationMap;

        public ShardStore()
        {
            _expirationMap = new ExpirationMap();
            _shards = Enumerable
                .Range(0, ShardMaxIndex)
                .Select(x => new ItemStore(_expirationMap))
                .ToArray();
        }
        
        public object? Get(ulong hash)
        {
            return _shards[hash % ShardMaxIndex].Get(hash);
        }

        public long? Expiration(ulong hash)
        {
            return _shards[hash % ShardMaxIndex].Expiration(hash);
        }

        public void Push(Item item)
        {
            _shards[item.Key % ShardMaxIndex].Push(item);
        }

        public object? Delete(ulong hash)
        {
            return _shards[hash % ShardMaxIndex].Delete(hash);
        }

        public void Cleanup(IPolicy policy)
        {
            _expirationMap.Cleanup(this, policy);
        }

        public void Clear()
        {
            foreach (var shard in _shards)
                shard.Clear();
        }
    }
}
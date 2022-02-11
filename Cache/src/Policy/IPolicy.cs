using System;
using Cache.Ring;

namespace Cache.Policy
{
    public interface IPolicy : IRingConsumer, IAsyncDisposable
    {
        /// <summary>
        ///     key-costペアの追加を行います。
        /// </summary>
        /// <remarks></remarks>
        /// <returns>evicted keyの配列を返します。ペアを追加できなかった場合 <c>null</c> を返します。</returns>
        (Item[]?, bool) Add(ulong key, long value);

        /// <summary>
        ///     policyにkeyを保持しているかを確認します。
        /// </summary>
        bool Has(ulong key);

        /// <summary>
        ///     policyからkeyを削除します。
        /// </summary>
        void Delete(ulong key);

        /// <summary>
        ///     利用可能な容量を取得します。
        /// </summary>
        ulong Capacity();

        /// <summary>
        ///     keyのcostを更新します。
        /// </summary>
        void Update(ulong key, long value);

        /// <summary>
        ///     keyのcostを取得します。
        /// </summary>
        long Cost(ulong key);

        /// <summary>
        ///     全てのコストをクリアします。
        /// </summary>
        void Clear();
    }
}
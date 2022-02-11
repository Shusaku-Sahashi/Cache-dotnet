using System.Linq;
using Cache.Core;
using Cache.Store;
using NUnit.Framework;

namespace Cache.Test.Store
{
    [TestFixture]
    public class ShardStoreFeature
    {
        [Test]
        public void Blank()
        {
            Assert.Pass();
        }

        [Test]
        public void GetItem()
        {
            var store = new ShardStore();
            var key = UtilAlg.KyeToHash(1);
            var item = new Item {Key = key, Value = 1,};
            store.Push(item);
            Assert.That(store.Get(key), Is.EqualTo(1));

            item.Value = 2;
            store.Push(item);
            Assert.That(store.Get(key), Is.EqualTo(2));

            key = UtilAlg.KyeToHash(1);
            item = new Item {Key = key, Value = 3};
            store.Push(item);
            Assert.That(store.Get(key), Is.EqualTo(3));
        }

        [Test]
        public void StoreDelete()
        {
            var store = new ShardStore();
            var key = UtilAlg.KyeToHash(1);

            var item = new Item {Key = key, Value = 1};

            store.Push(item);
            store.Delete(item.Key);

            var actual = store.Get(item.Key);
            Assert.Null(actual);
        }

        [Test]
        public void StoreClear()
        {
            var store = new ShardStore();
            Enumerable.Range(0, 1000).ToList().ForEach(i =>
            {
                var key = UtilAlg.KyeToHash(i);
                var item = new Item {Key = key, Value = i,};
                store.Push(item);
            });

            store.Clear();

            Enumerable.Range(0, 1000).ToList().ForEach(i =>
            {
                var key = UtilAlg.KyeToHash(i);
                var actual = store.Get(key);
                Assert.Null(actual);
            });
        }
    }
}
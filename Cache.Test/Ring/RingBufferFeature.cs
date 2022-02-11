using System;
using System.Linq;
using Cache.Ring;
using NUnit.Framework;

namespace Cache.Test.Ring
{
    [TestFixture]
    public class RingBufferFeature
    {
        [Test]
        public void RingDrain()
        {
            var drain = 0;
            var cons = new TestConsumer
            {
                Func = _ => drain++,
                Save = true,
            };

            var buffer = new RingBuffer(cons, 1);

            Enumerable.Range(0, 100).ToList().ForEach(i => { buffer.Push((ulong) i); });

            Assert.That(drain, Is.EqualTo(100));
        }

        [Test]
        public void RingReset()
        {
            var drain = 0;
            var cons = new TestConsumer
            {
                Func = _ => drain++,
                Save = false,
            };

            var buffer = new RingBuffer(cons, 1);

            Enumerable.Range(0, 100).ToList().ForEach(i => { buffer.Push((ulong) i); });

            Assert.That(drain, Is.EqualTo(0));
        }

        private class TestConsumer : IRingConsumer
        {
            public Action<ulong[]> Func { init; private get; }

            public bool Save { init; private get; }

            public bool Push(ulong[] data)
            {
                if (!Save) return false;

                Func(data);
                return true;
            }
        }
    }
}
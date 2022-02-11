using Microsoft.Extensions.ObjectPool;

namespace Cache.Ring
{
    internal class RingBuffer
    {
        private readonly ObjectPool<RingStripe> _pool;

        public RingBuffer(IRingConsumer consumer, int capacity)
        {
            _pool = new DefaultObjectPool<RingStripe>(new RingStripeObjectPolicy(consumer, capacity));
        }

        public void Push(ulong key)
        {
            var ring = _pool.Get();
            ring.Push(key);
            _pool.Return(ring);
        }

        private class RingStripeObjectPolicy : IPooledObjectPolicy<RingStripe>
        {
            private readonly IRingConsumer _consumer;
            private readonly int _capacity;

            public RingStripeObjectPolicy(IRingConsumer consumer, int capacity)
            {
                _consumer = consumer;
                _capacity = capacity;
            }

            public RingStripe Create() => new(_consumer, _capacity);

            public bool Return(RingStripe obj) => true;
        }
    }
}
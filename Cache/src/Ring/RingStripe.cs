using System.Collections.Generic;

namespace Cache.Ring
{
    internal class RingStripe
    {
        private IRingConsumer Consumer { get;}
        private List<ulong> Data { get; set; }
        private int Capacity { get;}

        public RingStripe(IRingConsumer consumer, int capacity)
        {
            this.Consumer = consumer;
            this.Capacity = capacity;
            Data = new List<ulong>(capacity + 10);
        }

        public void Push(ulong item)
        {
            Data.Add(item);
            if (Data.Count >= Capacity)
            {
                Data = Consumer.Push(Data.ToArray()) 
                    ? new List<ulong>(Capacity + 10) 
                    : Data.GetRange(0, 0);
            }
        }
    }
}
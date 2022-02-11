namespace Cache
{
    public class Item
    {
        public ItemFlag Flag { get; set; }
        public ulong Key { get; set; }
        public object Value { get; set; }
        public long Cost { get; set; }
        public long Expiration { get; set; }
    }

    public enum ItemFlag
    {
        ItemNew,
        ItemDelete,
        ItemUpdate
    }
}
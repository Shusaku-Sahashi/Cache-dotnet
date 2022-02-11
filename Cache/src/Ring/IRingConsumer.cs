using System.Threading.Tasks;

namespace Cache.Ring
{
    public interface IRingConsumer
    {
        bool Push(ulong[] data);
    }
}
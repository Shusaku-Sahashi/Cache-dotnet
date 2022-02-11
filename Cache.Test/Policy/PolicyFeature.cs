using System.Threading.Tasks;
using Cache.Policy;
using NUnit.Framework;

namespace Cache.Test.Policy
{
    [TestFixture]
    public class PolicyFeature
    {
        [Test]
        public async Task PolicyProcessItem()
        {
            await using var p = new DefaultPolicy(100, 10);
            await p.ItemChan.Writer.WriteAsync(new DefaultPolicy.ItemMessage(new ulong[] {1, 2, 2}));

            await Task.Delay(10);
            
            Assert.That(p.Admit.Estimate(2), Is.EqualTo(2L));
            Assert.That(p.Admit.Estimate(1), Is.EqualTo(1L));

            await p.ItemChan.Writer.WriteAsync(new DefaultPolicy.ItemMessage(new ulong[] {3, 3, 3}));

            await Task.Delay(10);
            Assert.That(p.Admit.Estimate(3), Is.EqualTo(3L));
        }
    }
}
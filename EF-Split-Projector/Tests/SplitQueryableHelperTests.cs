using System.Linq;
using EF_Split_Projector;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class SplitQueryableHelperTests : IntegratedTestsBase
    {
        [Test]
        public void Test()
        {
            var splitQuery = TestHelper.Context.Inventory.Select(SelectInventory()).SplitSelect(4);
            Assert.IsNotNull(splitQuery);
        }
    }
}
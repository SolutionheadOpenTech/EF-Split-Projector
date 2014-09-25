using System;
using System.Linq;
using EF_Split_Projector.Helpers.Visitors;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class QueryableSplitterHelperTests : IntegratedTestsBase
    {
        [Test]
        public void Test()
        {
            var originalQuery = TestHelper.Context.Inventory.Select(SelectInventory());

            var shards = QueryableSplitterHelper.Split(originalQuery, 2);
            foreach(var shard in shards)
            {
                Console.WriteLine(shard.Expression);
            }
        }
    }
}
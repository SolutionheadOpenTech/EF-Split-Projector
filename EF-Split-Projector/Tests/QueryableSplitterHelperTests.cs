using System;
using System.Linq;
using EF_Split_Projector.Helpers;
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

            var shards = QueryableSplitterHelper.Split(originalQuery, 4);
            foreach(var shard in shards)
            {
                Console.WriteLine("Shard:");
                Console.WriteLine(shard.Expression);
                Console.WriteLine("----------------------");
            }
        }
    }
}
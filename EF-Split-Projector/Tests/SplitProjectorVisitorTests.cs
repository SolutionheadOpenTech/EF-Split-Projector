using System;
using System.Linq;
using EF_Split_Projector.Helpers.Visitors;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class SplitProjectorVisitorTests : IntegratedTestsBase
    {
        [Test]
        public void Test()
        {
            var selectInventory = SelectInventory();
            var originalQuery = TestHelper.Context.Inventory
                                          .Select(selectInventory);
            var shattered = ShatterOnMemberInitVisitor.ShatterExpression(originalQuery.Expression);
            var count = 0;
            foreach(var shard in shattered.Shards)
            {
                Console.WriteLine("Shard {0}:", count++);
                Console.WriteLine(shard);
                Console.WriteLine("-------------------");
            }
            Assert.Pass();
        }
    }
}
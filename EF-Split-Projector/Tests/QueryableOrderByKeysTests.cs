using System;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Visitors;
using NUnit.Framework;
using Tests.TestContext.DataModels;

namespace Tests
{
    [TestFixture]
    public class QueryableOrderByKeysTests
    {
        public class InventorySelect
        {
            public int Sequence { get; set; }
        }

        public Expression<Func<Inventory, InventorySelect>> SelectInventory()
        {
            return i => new InventorySelect
                {
                    Sequence = i.DateSequence
                };
        }

        [Test]
        public void Test()
        {
            var selectInventory = SelectInventory();
            using(var testContext = new TestContext.TestContext())
            {
                var originalQuery = testContext.Inventory.Where(i => i.DateSequence > 0)
                    //.OrderBy(i => i.LotDateCreated)
                    .Select(selectInventory);
                var newQuery = OrderByKeysVisitor.InjectOrderByEntityKeys(originalQuery);

                Console.WriteLine("OriginalQuery:");
                Console.WriteLine(Pretify(originalQuery.Expression.ToString()));
                Console.WriteLine("\n--------------------------------------\n");
                Console.WriteLine("NewQuery:");
                Console.WriteLine(Pretify(newQuery.Expression.ToString()));

                Assert.IsNotNull(newQuery);

                var results = newQuery.ToList();
                Assert.IsNotNull(results);
            }
        }

        public string Pretify(string source)
        {
            var result = source.Replace("{", "\n{\n");
            result = result.Replace(",", ",\n");
            return result;
        }
    }
}
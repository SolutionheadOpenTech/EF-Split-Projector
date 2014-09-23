using System;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Visitors;
using NUnit.Framework;
using Tests.TestContext;
using Tests.TestContext.DataModels;

namespace Tests
{
    [TestFixture]
    public class QueryableOrderByKeysTests
    {
        public class InventorySelect
        {
            public string ItemDescription { get; set; }

            public string Location { get; set; }

            public int Quantity { get; set; }
        }

        public Expression<Func<Inventory, InventorySelect>> SelectInventory()
        {
            return i => new InventorySelect
                {
                    ItemDescription = i.Item.Description,
                    Location = i.Location.Description,
                    Quantity = i.Quantity
                };
        }

        [Test]
        public void Test()
        {
            var selectInventory = SelectInventory();
            using(var testContext = new TestDatabase())
            {
                var originalQuery = testContext.Inventory
                    .Where(i => i.Quantity > 0)
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
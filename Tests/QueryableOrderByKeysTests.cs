using System;
using System.Linq;
using EF_Split_Projector.Helpers.Visitors;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class QueryableOrderByKeysTests : IntegratedTestsBase
    {
        [Test]
        public void Test()
        {
            var selectInventory = SelectInventory();
            
            var originalQuery = TestHelper.Context.Inventory
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

        [Test]
        public void Adds_Skip_method_call_to_expression()
        {
            var selectInventory = SelectInventory();

            var originalQuery = TestHelper.Context.Inventory
                .Where(i => i.Quantity > 0)
                .Select(selectInventory);
            var newQuery = OrderByKeysVisitor.InjectOrderByEntityKeys(originalQuery);
            Assert.IsTrue(newQuery.Expression.ToString().Contains("Skip(0)"));

            var results = newQuery.ToList();
            Assert.IsNotNull(results);
        }

        public string Pretify(string source)
        {
            var result = source.Replace("{", "\n{\n");
            result = result.Replace(",", ",\n");
            return result;
        }
    }
}
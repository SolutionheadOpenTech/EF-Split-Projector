using System.Linq;
using EF_Split_Projector.Helpers;
using NUnit.Framework;
using Tests.Helpers;
using Tests.TestContext.DataModels;

namespace Tests
{
    [TestFixture]
    public class BatchQueryTests : IntegratedTestsBase
    {
        [Test]
        public void BatchingQueriesReturnsExpectedResults()
        {
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();

            var query1 = TestHelper.Context.Inventory.Select(i => new InventorySelect
                {
                    ItemDescription = i.Item.Description
                });
            var query2 = TestHelper.Context.Inventory.Select(i => new InventorySelect
                {
                    Quantity = i.Quantity
                });

            var expected1 = query1.ToList();
            var expected2 = query2.ToList();

            var results = BatchQueriesHelper.ExecuteBatchQueries(query1, query2);

            EquivalentHelper.AreEquivalent(expected1, results[0]);
            EquivalentHelper.AreEquivalent(expected2, results[1]);
        }
    }
}
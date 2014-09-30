using System.Linq;
using EF_Split_Projector;
using NUnit.Framework;
using Tests.TestContext.DataModels;

namespace Tests
{
    [TestFixture]
    public class SplitQueryableHelperTests : IntegratedTestsBase
    {
        [Test]
        public void Returns_data_as_expected()
        {
            //Arrange
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();

            //Act
            var queryable = TestHelper.Context.Inventory.Select(SelectInventory());
            var expected = queryable.ToList();
            var split = queryable.AsSplitQueryable(4).ToList();

            //Assert
            AssertEqual(expected, split);
        }
    }
}
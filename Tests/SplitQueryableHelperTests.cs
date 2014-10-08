using System;
using System.Data.Entity;
using System.Linq;
using EF_Split_Projector;
using NUnit.Framework;
using Tests.Helpers;
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
            Assert.IsTrue(EquivalentHelper.AreEquivalent(expected, split));
        }

        [Test]
        public void QueryableTest()
        {
            //Arrange
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();

            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Packaging>();
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Packaging>();
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Packaging>();

            //Act
            var select = SelectInventoryWithPackagings(TestHelper.Context.Packaging);
            var queryable = TestHelper.Context.Inventory.Select(select);
            var expected = queryable.ToList();
            var split = queryable.AsSplitQueryable(4).ToList();

            //Assert
            Assert.IsTrue(EquivalentHelper.AreEquivalent(expected, split));
        }

        [Test]
        public void ToListAsync()
        {
            //Arrange
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();

            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Packaging>();
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Packaging>();
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Packaging>();

            //Act
            var select = SelectInventoryWithPackagings(TestHelper.Context.Packaging);
            var queryable = TestHelper.Context.Inventory.Select(select);
            var expected = queryable.ToList();
            
            var listTask = queryable.AsSplitQueryable(4).ToListAsync();
            if(!listTask.IsCompleted)
            {
                Console.WriteLine("Ran task, now waiting...");
                listTask.Wait();
                Console.WriteLine("finished waiting, ToListAsynch works!");
            }
            else
            {
                Assert.Fail("Task finished before continuing execution.");
            }

            //Assert
            Assert.IsTrue(EquivalentHelper.AreEquivalent(expected, listTask.Result));
        }
    }
}
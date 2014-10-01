﻿using System.Linq;
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
    }
}
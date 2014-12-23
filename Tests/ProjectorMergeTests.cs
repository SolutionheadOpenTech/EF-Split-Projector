using System;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Extensions;
using NUnit.Framework;
using Tests.Helpers;
using Tests.TestContext.DataModels;

namespace Tests
{
    [TestFixture]
    public class ProjectorMergeTests : IntegratedTestsBase
    {
        [Test]
        public void Returns_merged_projector_results_as_expected()
        {
            //Arrange
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();

            //Act
            var expected = TestHelper.Context.Inventory.Select(SelectInventory()).ToList();

            var mergedProjector = ProjectorMerge.Merge(SelectInventory0(), SelectInventory1());
            var result = TestHelper.Context.Inventory.Select(mergedProjector).ToList();

            //Assert
            EquivalentHelper.AreEquivalent(expected, result);
        }

        private Expression<Func<Inventory, InventorySelect>> SelectInventory0()
        {
            return i => new InventorySelect
            {
                WarehouseLocations = i.Location.Warehouse.Locations.Select(l => new WarehouseLocationSelect
                {
                    Warehouse = l.Warehouse.Name,
                }),

                ItemDescription = i.Item.Description,
            };
        }

        private Expression<Func<Inventory, InventorySelect>> SelectInventory1()
        {
            return i => new InventorySelect
            {
                WarehouseLocations = i.Location.Warehouse.Locations.Select(l => new WarehouseLocationSelect
                {
                    Location = l.Description
                }),

                Location = i.Location.Description,
                Quantity = i.Quantity
            };
        }
    }
}

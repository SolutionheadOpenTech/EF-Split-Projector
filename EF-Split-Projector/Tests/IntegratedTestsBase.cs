using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using Tests.TestContext;
using Tests.TestContext.DataModels;

namespace Tests
{
    [TestFixture]
    public abstract class IntegratedTestsBase
    {
        public TestHelper TestHelper { get; set; }

        [SetUp]
        public void SetUp()
        {
            TestHelper = new TestHelper();
            TestHelper.ResetContext();
        }

        [TearDown]
        public void TearDown()
        {
            TestHelper.DisposeOfContext();
        }

        public class InventorySelect
        {
            public string ItemDescription { get; set; }
            public string Location { get; set; }
            public int Quantity { get; set; }
            public IEnumerable<WarehouseLocationSelect> WarehouseLocations { get; set; }
        }

        public class WarehouseLocationSelect
        {
            public string Warehouse { get; set; }
            public string Location { get; set; }
        }

        public Expression<Func<Inventory, InventorySelect>> SelectInventory()
        {
            return i => new InventorySelect
                {
                    WarehouseLocations = i.Location.Warehouse.Locations.Select(l => new WarehouseLocationSelect
                        {
                            Warehouse = l.Warehouse.Name,
                            Location = l.Description
                        }),

                    ItemDescription = i.Item.Description,
                    Location = i.Location.Description,

                    Quantity = i.Quantity
                };
        }

        public Expression<Func<Inventory, InventorySelect>> SelectInventory0()
        {
            return i => new InventorySelect
                {
                    WarehouseLocations = i.Location.Warehouse.Locations.Select(l => new WarehouseLocationSelect
                        {
                            Warehouse = l.Warehouse.Name,
                        })
                };
        }

        public Expression<Func<Inventory, InventorySelect>> SelectInventory1()
        {
            return i => new InventorySelect
                {
                    WarehouseLocations = i.Location.Warehouse.Locations.Select(l => new WarehouseLocationSelect
                        {
                            Location = l.Description
                        })
                };
        }

        public Expression<Func<Inventory, InventorySelect>> SelectInventory2()
        {
            return i => new InventorySelect
                {
                    ItemDescription = i.Item.Description,
                };
        }

        public Expression<Func<Inventory, InventorySelect>> SelectInventory3()
        {
            return i => new InventorySelect
                {
                    Location = i.Location.Description,
                };
        }

        public Expression<Func<Inventory, InventorySelect>> SelectInventory4()
        {
            return i => new InventorySelect
                {
                    Quantity = i.Quantity
                };
        }
    }
}
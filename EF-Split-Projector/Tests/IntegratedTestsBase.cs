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

        public class InventoryAndPackaging
        {
            public string Item { get; set; }
            public IEnumerable<double> PackagingWeights { get; set; }
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

        public Expression<Func<Inventory, InventoryAndPackaging>> SelectInventoryWithPackagings(IQueryable<Packaging> packagings)
        {
            return i => new InventoryAndPackaging
                {
                    Item = i.Item.Description,
                    PackagingWeights = packagings.Where(p => p.Id == i.ItemId).Select(p => p.Weight)
                };
        }
    }
}
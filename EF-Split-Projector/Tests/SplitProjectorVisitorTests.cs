using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Visitors;
using NUnit.Framework;
using Tests.TestContext.DataModels;

namespace Tests
{
    [TestFixture]
    public class SplitProjectorVisitorTests : IntegratedTestsBase
    {
        public Expression<Func<Inventory, InventorySelect>> ExpectedProjector0()
        {
            return i => new InventorySelect
            {
                WarehouseLocations = i.Location.Warehouse.Locations.Select(l => new WarehouseLocationSelect
                {
                    Warehouse = l.Warehouse.Name,
                })
            };
        }

        public Expression<Func<Inventory, InventorySelect>> ExpectedProjector1()
        {
            return i => new InventorySelect
            {
                WarehouseLocations = i.Location.Warehouse.Locations.Select(l => new WarehouseLocationSelect
                {
                    Location = l.Description
                })
            };
        }

        public Expression<Func<Inventory, InventorySelect>> ExpectedProjector2()
        {
            return i => new InventorySelect
            {
                ItemDescription = i.Item.Description,
            };
        }

        public Expression<Func<Inventory, InventorySelect>> ExpectedProjector3()
        {
            return i => new InventorySelect
            {
                Location = i.Location.Description,
            };
        }

        public Expression<Func<Inventory, InventorySelect>> ExpectedProjector4()
        {
            return i => new InventorySelect
            {
                Quantity = i.Quantity
            };
        }

        [Test]
        public void ShattersProjectorAsExpected()
        {
            var originalQuery = TestHelper.Context.Inventory.Select(SelectInventory());
            var expectedProjectors = new List<string>
                {
                    TestHelper.Context.Inventory.Select(ExpectedProjector0()).Expression.ToString(),
                    TestHelper.Context.Inventory.Select(ExpectedProjector1()).Expression.ToString(),
                    TestHelper.Context.Inventory.Select(ExpectedProjector2()).Expression.ToString(),
                    TestHelper.Context.Inventory.Select(ExpectedProjector3()).Expression.ToString(),
                    TestHelper.Context.Inventory.Select(ExpectedProjector4()).Expression.ToString()
                };

            var expressions = ShatterOnMemberInitVisitor.ShatterExpression(originalQuery.Expression).Shards.Select(s => s.ToString()).ToList();
            
            Assert.AreEqual(expectedProjectors.Count, expressions.Count);
            var count = 0;
            foreach(var expected in expectedProjectors)
            {
                Console.WriteLine("Shard {0}:", count++);
                Console.WriteLine(expected);
                Console.WriteLine("------------------------");
                Assert.IsTrue(expressions.Contains(expected));
            }
        }
    }
}
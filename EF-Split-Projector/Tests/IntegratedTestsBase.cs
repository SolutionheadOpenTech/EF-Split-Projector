using System;
using System.Collections;
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

        internal static bool AssertEqual(object expected, object result)
        {
            if(expected == null)
            {
                return result == null;
            }

            if(result == null)
            {
                return false;
            }

            var expectedEnumerable = expected as IEnumerable;
            if(expectedEnumerable != null)
            {
                return AssertEqual(((IEnumerable)expected).Cast<object>().ToList(), ((IEnumerable)result).Cast<object>().ToList());
            }

            var expectedType = expected.GetType();
            var resultType = result.GetType();

            var expectedProperties = expectedType.GetProperties()
                .Select(p => p.GetGetMethod())
                .Where(m => m != null).ToList();
            var resultProperties = expectedProperties.Select(e => resultType.GetProperty(e.Name))
                .Where(p => p != null)
                .Select(p => p.GetGetMethod())
                .Where(m => m != null).ToDictionary(m => m.Name, m => m);
            if(expectedProperties.Count != resultProperties.Count)
            {
                return false;
            }
            if(expectedProperties.Any(e => !AssertEqual(e.Invoke(expected, null), resultProperties[e.Name].Invoke(result, null))))
            {
                return false;
            }

            var expectedFields = expectedType.GetFields().ToList();
            var resultFields = expectedFields.Select(e => resultType.GetField(e.Name))
                .Where(p => p != null).ToDictionary(m => m.Name, m => m);
            if(expectedFields.Count != resultFields.Count)
            {
                return false;
            }
            return expectedFields.All(e => AssertEqual(e.GetValue(expected), resultFields[e.Name].GetValue(result)));
        }

        private static bool AssertEqual(List<object> expected, List<object> result)
        {
            if(expected.Count != result.Count)
            {
                return false;
            }

            return !expected.Any(e => result.All(r => !AssertEqual(e, r)));
        }
    }
}
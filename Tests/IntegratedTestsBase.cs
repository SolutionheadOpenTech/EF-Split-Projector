using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EF_Projectors;
using EF_Split_Projector;
using EF_Split_Projector.Helpers.Extensions;
using LinqKit;
using NUnit.Framework;
using Tests.TestContext;
using Tests.TestContext.DataModels;

namespace Tests
{
    public class MethodInfoTest
    {
        public class Banana<TSomething, TList> : List<TList>
        {
            
        }

        public static void Method<T, T2>(T p, List<T2> list)
        {
            
        }

        private static readonly MethodInfo MethodInfo = typeof(MethodInfoTest).GetMethod("Method");

        [Test]
        public void Test()
        {
            var morphed = MethodInfo.UpdateGenericArguments(new List<Type>
                {
                    typeof(int),
                    typeof(Banana<double, string>)
                });

            Assert.IsNotNull(morphed);
            Assert.AreEqual(MethodInfo, morphed.GetGenericMethodDefinition());
        }
    }

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
            TestHelper.Reset();
        }

        public interface IInventorySelect
        {
            string ItemDescription { get; set; }
        }

        public sealed class InventorySelect : IInventorySelect
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

    public class PickedInventoryTests : IntegratedTestsBase
    {
        public class OrderReturn
        {
            public DateTime DateCreated { get; set; }
            public int Sequence { get; set; }
            public DetailReturn Detail { get; set; }
        }

        public class DetailReturn
        {
            public PickedInventoryReturn PickedInventory { get; set; }
        }

        public class PickedInventoryReturn
        {
            public PickedInventoryKeyReturn Key { get; set; }
            public IEnumerable<PickedInventoryItemReturn> Items { get; set; }
        }

        public class PickedInventoryKeyReturn
        {
            public DateTime DateCreated { get; set; }
            public int Sequence { get; set; }
        }

        public class PickedInventoryItemReturn
        {
            public int Quantity { get; set; }
        }

        [Test]
        public void Test()
        {
            var order = TestHelper.CreateObjectGraphAndInsertIntoDatabase<Order>();

            var select = new Projectors<Order, OrderReturn>
                {
                    o => new OrderReturn
                        {
                            DateCreated = o.DateCreated
                        },
                    {
                        new Projectors<PickedInventory, PickedInventoryReturn>
                            {
                                p => new PickedInventoryReturn
                                    {
                                        Key = new PickedInventoryKeyReturn
                                            {
                                                DateCreated = p.DateCreated,
                                                Sequence = p.DateSequence
                                            }
                                    },
                                p => new PickedInventoryReturn
                                    {
                                        Items = p.Items.Select(i => new PickedInventoryItemReturn
                                            {
                                                Quantity = i.Quantity
                                            })
                                    }
                            },
                        s => o => new OrderReturn
                            {
                                Detail = new DetailReturn
                                    {
                                        PickedInventory = s.Invoke(o.PickedInventory)
                                    }
                            }
                    }
                };

            var result = TestHelper.Context.Orders.SplitSelect(select).FirstOrDefault();
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Detail.PickedInventory.Key);
            Assert.IsNotNull(result.Detail.PickedInventory.Items);

        }
    }
}
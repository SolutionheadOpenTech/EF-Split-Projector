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
        public class Banana<TSomething, TList> : List<TList> { }

        public static void Method<T, T2>(T p, List<T2> list) { }

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

    public class MergeOrderTests : IntegratedTestsBase
    {
        public class ProductionReturn
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public IEnumerable<InventoryReturn> Results { get; set; }
        }

        public class InventoryReturn
        {
            public DateTime Date { get; set; }
            public int Sequence { get; set; }

            public string Item { get; set; }
            public int Quantity { get; set; }
        }

        [Test]
        /*
         * In order for items to be merged as expected we need to ensure that the split queries are returning all items across the graph in the same order.
         * We accomplish this by ordering all entities according to their key by injecting OrderBy/ThenBy expressions on all references to a colletion of entities
         * accross the projectors.
         * 
         * This becomes a problem in certain cases because EF may *drop* the ordering when it deems it irrelevant (presumably to produce more
         * efficient SQL) and parts of items may end up getting merged with *different* items in a collection. Such is the case with SelectMany:
         * If we have A going to many B which in turn goes to many C and project A => A.B.SelectMany(b => b.C) we will inject calls to order the keys like this:
         * A => A.B.OrderBy(keys).SelectMany(b => b.C.OrderBy(keys)) but when EF converts the expression to SQL it will *not* preserve those OrderBy expressions,
         * apparently because it is not part of the spec that Select/SelectMany operations will return results in a any particular order. And indeed, the order
         * of items *can* end up differing between split queries, which means we end up merging the wrong items together.
         * 
         * The typical solution out there is to append the ordering *after* the select like so: A => A.B.SelectMany(b => b.C).OrderBy(...), but since we need
         * to order by *entity keys* and have no way to guarantee that the select isn't actually returning a different object altogether, this won't work.
         * Instead, we've discovered that we can *force* EF to acknowledge the order we've stated by appending it with Skip(0):
         * A => A.B.OrderBy(keys).Skip(0).SelectMany(b => b.C.OrderBy(keys).Skip(0)).
         * Since a call to Skip modifies the collection we're dealing with *according* to the order we've specified, EF has no choice but to actually have to
         * enforce the ordering; but since we're passing 0, we're actually not skipping anything and end up working with the entire set as expected. Thankfully
         * EF isn't trying to be *so* smart that it accounts for 0 elements skipped, and hopefully that will not change in subsequent versions.
         * 
         * This test attempst to address the issue, but since we're dealing with undetermined behaviour it's impossible to guarantee its consitency in an
         * integrated context. If we remove the injection of the Skip(0) calls it's still technically possible to get false verification if the data
         * *just happens* to be returned in the right order, though it never happened in my testing. However it shouldn't be possible to get a false failure,
         * so it at least alerts us of that much.
         *     -RI 2016-5-23
         */
        public void Property_assigned_by_SelectMany_are_merged_appropriately()
        {
            var schedule = TestHelper.CreateObjectGraphAndInsertIntoDatabase<ProductionSchedule>(s => s.Productions = new List<Production>
                {
                    TestHelper.CreateObjectGraph<Production>(r => r.Results = new List<Inventory>()),
                    TestHelper.CreateObjectGraph<Production>(r => r.Results = new List<Inventory>()),
                    TestHelper.CreateObjectGraph<Production>(r => r.Results = new List<Inventory>())
                });
            var productions = schedule.Productions.ToList();

            for(var i = 0; i < 20; ++i)
            {
                var item = TestHelper.CreateObjectGraph<Inventory>();
                productions[i % 3].Results.Add(item);
            }
            TestHelper.SaveChangesToContext();

            var expected = schedule.Productions
                .OrderBy(r => r.DataCreated).ThenBy(r => r.Sequence)
                .SelectMany(r => r.Results.OrderBy(i => i.DateCreated).ThenBy(i => i.DateSequence))
                .ToList();

            var select = new Projectors<ProductionSchedule, ProductionReturn>
                {
                    p => new ProductionReturn
                        {
                            Start = p.Start,
                            End = p.End
                        },
                    p => new ProductionReturn
                        {
                            Results = p.Productions.SelectMany(r => r.Results.Select(i => new InventoryReturn
                                {
                                    Date = i.DateCreated,
                                    Sequence = i.DateSequence
                                }))
                        },
                    p => new ProductionReturn
                        {
                            Results = p.Productions.SelectMany(r => r.Results.Select(i => new InventoryReturn
                                {
                                    Item = i.Item.Description,
                                    Quantity = i.Quantity
                                }))
                        }
                };

            using(var context = new TestDatabase())
            {
                var result = context.ProductionSchedules.SplitSelect(select).FirstOrDefault();
                AssertEqual(expected, result.Results);
            }
        }

        private static void AssertEqual(IEnumerable<Inventory> expected, IEnumerable<InventoryReturn> results)
        {
            Assert.AreEqual(expected.Count(), results.Count());
            
            using(var expectedEnumerator = expected.GetEnumerator())
            using(var resultsEnumerator = results.GetEnumerator())
            {
                while(expectedEnumerator.MoveNext() && resultsEnumerator.MoveNext())
                {
                    AssertEqual(expectedEnumerator.Current, resultsEnumerator.Current);
                }
            }
        }

        private static void AssertEqual(Inventory expected, InventoryReturn result)
        {
            Assert.AreEqual(expected.DateCreated, result.Date);
            Assert.AreEqual(expected.DateSequence, result.Sequence);
            Assert.AreEqual(expected.Item.Description, result.Item);
            Assert.AreEqual(expected.Quantity, result.Quantity);
        }
    }
}
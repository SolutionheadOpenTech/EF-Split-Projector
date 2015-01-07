using System;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using Tests.TestContext.DataModels;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Where_Interface : LINQQueryableMethodTestBase<Inventory, IntegratedTestsBase.InventorySelect, IntegratedTestsBase.IInventorySelect>
    {
        protected override IQueryable<IInventorySelect> GetQuery(IQueryable<InventorySelect> source)
        {
            var description = source.Select(i => i.ItemDescription).First();
            return ((IQueryable<IInventorySelect>)source).Where(i => i.ItemDescription != description);
        }

        protected override IQueryable<Inventory> Source {  get { return TestHelper.Context.Inventory; } }

        protected override Expression<Func<Inventory, InventorySelect>> Projector { get { return SelectInventory(); } }
    }
}
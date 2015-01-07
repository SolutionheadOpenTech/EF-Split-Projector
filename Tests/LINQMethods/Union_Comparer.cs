using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Union_Comparer : LINQQueryableInventoryMethodTestBase<IntegratedTestsBase.InventorySelect>
    {
        protected override IQueryable<InventorySelect> GetQuery(IQueryable<InventorySelect> source)
        {
            var union = new List<InventorySelect>
                {
                    new InventorySelect()
                };
            return source.Union(union, new TestComparer<InventorySelect>());
        }
    }
}
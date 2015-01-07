using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Intersect_Comparer : LINQQueryableInventoryMethodTestBase<IntegratedTestsBase.InventorySelect>
    {
        protected override IQueryable<InventorySelect> GetQuery(IQueryable<InventorySelect> source)
        {
            var intersect = new List<InventorySelect>
                {
                    new InventorySelect()
                };
            return source.Intersect(intersect, new TestComparer<InventorySelect>());
        }
    }
}
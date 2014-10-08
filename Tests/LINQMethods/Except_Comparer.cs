using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Except_Comparer : LINQQueryableMethodTestBase<IntegratedTestsBase.InventorySelect>
    {
        protected override IQueryable<InventorySelect> GetQuery(IQueryable<InventorySelect> source)
        {
            var except = new List<InventorySelect>
                {
                    new InventorySelect()
                };
            return source.Except(except, new TestComparer<InventorySelect>());
        }
    }
}
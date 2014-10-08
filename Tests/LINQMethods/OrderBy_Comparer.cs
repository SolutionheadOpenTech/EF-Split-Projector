using System.Linq;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class OrderBy_Comparer : LINQQueryableMethodTestBase<IntegratedTestsBase.InventorySelect>
    {
        protected override IQueryable<InventorySelect> GetQuery(IQueryable<InventorySelect> source)
        {
            return source.OrderBy(s => s.Quantity, new TestComparer<int>());
        }
    }
}
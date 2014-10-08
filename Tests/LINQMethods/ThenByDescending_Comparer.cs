using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class ThenByDescending_Comparer : LINQQueryableMethodTestBase<IntegratedTestsBase.InventorySelect>
    {
        protected override IQueryable<InventorySelect> GetQuery(IQueryable<InventorySelect> source)
        {
            return source.OrderBy(s => s.Quantity).ThenByDescending(s => s.Location, new NUnitComparer<string>());
        }
    }
}
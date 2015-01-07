using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class ThenBy_Comparer : LINQQueryableInventoryMethodTestBase<IntegratedTestsBase.InventorySelect>
    {
        protected override IQueryable<InventorySelect> GetQuery(IQueryable<InventorySelect> source)
        {
            return source.OrderBy(s => s.Quantity).ThenBy(s => s.Location, new NUnitComparer<string>());
        }
    }
}
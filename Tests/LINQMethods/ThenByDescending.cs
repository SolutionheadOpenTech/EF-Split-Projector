using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class ThenByDescending : LINQQueryableMethodTestBase<IntegratedTestsBase.InventorySelect>
    {
        protected override IQueryable<InventorySelect> GetQuery(IQueryable<InventorySelect> source)
        {
            return source.OrderBy(s => s.Quantity).ThenByDescending(s => s.Location);
        }
    }
}
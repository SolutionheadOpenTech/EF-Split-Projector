using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Where_ReferencingChild : LINQQueryableMethodTestBase<IntegratedTestsBase.InventorySelect>
    {
        protected override IQueryable<InventorySelect> GetQuery(IQueryable<InventorySelect> source)
        {
            return source.Where(i => i.WarehouseLocations.Count() > 1);
        }
    }
}
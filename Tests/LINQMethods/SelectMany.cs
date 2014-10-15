using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class SelectMany : LINQQueryableMethodTestBase<IntegratedTestsBase.WarehouseLocationSelect>
    {
        protected override IQueryable<WarehouseLocationSelect> GetQuery(IQueryable<InventorySelect> source)
        {
            return source.SelectMany(i => i.WarehouseLocations);
        }
    }
}
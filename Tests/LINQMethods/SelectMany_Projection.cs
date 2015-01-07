using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class SelectMany_Projection : LINQQueryableInventoryMethodTestBase<SelectMany_Projection.WarehouseLocationProjection>
    {
        public class WarehouseLocationProjection
        {
            public InventorySelect Inventory;
            public string Location;
        }

        protected override IQueryable<WarehouseLocationProjection> GetQuery(IQueryable<InventorySelect> source)
        {
            return source.SelectMany(i => i.WarehouseLocations, (i, l) => new WarehouseLocationProjection
                {
                    Inventory = i,
                    Location = l.Location
                });
        }
    }
}
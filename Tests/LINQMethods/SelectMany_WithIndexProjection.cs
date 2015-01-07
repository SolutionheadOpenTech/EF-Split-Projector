using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class SelectMany_WithIndexProjection : LINQQueryableInventoryMethodTestBase<SelectMany_WithIndexProjection.WarehouseLocationProjection>
    {
        public class WarehouseLocationProjection
        {
            public InventorySelect Inventory;
            public string Location;
            public int Index;
        }

        protected override IQueryable<WarehouseLocationProjection> GetQuery(IQueryable<InventorySelect> source)
        {
            return source.SelectMany((i, n) => i.WarehouseLocations.Select(l => new
                    {
                        Location = l,
                        Index = n
                    }),
                (i, l) => new WarehouseLocationProjection
                    {
                        Inventory = i,
                        Location = l.Location.Location,
                        Index = l.Index
                    });
        }
    }
}
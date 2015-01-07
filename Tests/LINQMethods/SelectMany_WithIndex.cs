using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class SelectMany_WithIndex : LINQQueryableInventoryMethodTestBase<SelectMany_WithIndex.LocationWithIndex>
    {
        public class LocationWithIndex
        {
            public WarehouseLocationSelect WarehouseLocation;
            public int Index;
        }

        protected override IQueryable<LocationWithIndex> GetQuery(IQueryable<InventorySelect> source)
        {
            return source.SelectMany((i, n) => i.WarehouseLocations.Select(l => new LocationWithIndex
                {
                    WarehouseLocation = l,
                    Index = n
                }));
        }
    }
}
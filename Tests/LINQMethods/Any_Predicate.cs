using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Any_Predicate : LINQSingularMethodTestBase<bool>
    {
        protected override bool GetResult(IQueryable<InventorySelect> source)
        {
            return source.Any(i => i.WarehouseLocations.Any() && i.Location != "banana");
        }
    }
}
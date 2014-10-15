using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Aggregate_SeedSelect : LINQSingularMethodTestBase<double>
    {
        protected override double GetResult(IQueryable<InventorySelect> source)
        {
            return source.Aggregate(new InventorySelect { }, (c, i) => c ?? i, i => i.Quantity);
        }
    }
}
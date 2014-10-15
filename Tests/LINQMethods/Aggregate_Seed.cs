using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Aggregate_Seed : LINQSingularMethodTestBase<IntegratedTestsBase.InventorySelect>
    {
        protected override InventorySelect GetResult(IQueryable<InventorySelect> source)
        {
            return source.Aggregate(new InventorySelect { }, (c, i) => c ?? i);
        }
    }
}
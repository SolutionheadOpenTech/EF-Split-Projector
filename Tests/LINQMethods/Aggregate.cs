using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Aggregate : LINQSingularMethodTestBase<IntegratedTestsBase.InventorySelect>
    {
        protected override InventorySelect GetResult(IQueryable<InventorySelect> source)
        {
            return source.Aggregate((c, i) => c ?? i);
        }
    }
}
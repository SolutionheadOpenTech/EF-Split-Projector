using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Min : LINQSingularMethodTestBase<IntegratedTestsBase.InventorySelect>
    {
        protected override InventorySelect GetResult(IQueryable<InventorySelect> source)
        {
            return source.Min();
        }
    }
}
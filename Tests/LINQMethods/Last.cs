using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Last : LINQSingularMethodTestBase<IntegratedTestsBase.InventorySelect>
    {
        protected override InventorySelect GetResult(IQueryable<InventorySelect> source)
        {
            return source.Last();
        }
    }
}
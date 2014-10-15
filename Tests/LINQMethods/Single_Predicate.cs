using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Single_Predicate : LINQSingularMethodTestBase<IntegratedTestsBase.InventorySelect>
    {
        protected override int TestRecords { get { return 1; } }

        protected override InventorySelect GetResult(IQueryable<InventorySelect> source)
        {
            return source.Single(i => i.Quantity > 0);
        }
    }
}
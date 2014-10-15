using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class SingleOrDefault : LINQSingularMethodTestBase<IntegratedTestsBase.InventorySelect>
    {
        protected override int TestRecords { get { return 1; } }

        protected override InventorySelect GetResult(IQueryable<InventorySelect> source)
        {
            return source.SingleOrDefault();
        }
    }
}
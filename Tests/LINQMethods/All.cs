using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class All : LINQSingularMethodTestBase<bool>
    {
        protected override bool GetResult(IQueryable<InventorySelect> source)
        {
            return source.All(i => i.Quantity > 0);
        }
    }
}
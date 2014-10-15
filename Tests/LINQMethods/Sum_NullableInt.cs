using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Sum_NullableInt : LINQSingularMethodTestBase<int?>
    {
        protected override int? GetResult(IQueryable<InventorySelect> source)
        {
            return source.Select(i => (int?)i.Quantity).Sum();
        }
    }
}
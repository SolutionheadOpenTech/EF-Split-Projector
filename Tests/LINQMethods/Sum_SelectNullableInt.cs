using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Sum_SelectNullableInt : LINQSingularMethodTestBase<int?>
    {
        protected override int? GetResult(IQueryable<InventorySelect> source)
        {
            return source.Sum(i => (int?)i.Quantity);
        }
    }
}
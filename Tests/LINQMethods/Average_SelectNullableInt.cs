using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Average_SelectNullableInt : LINQSingularMethodTestBase<double?>
    {
        protected override double? GetResult(IQueryable<InventorySelect> source)
        {
            return source.Average(i => (int?)i.Quantity);
        }
    }
}
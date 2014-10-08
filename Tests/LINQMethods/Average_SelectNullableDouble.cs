using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Average_SelectNullableDouble : LINQSingularMethodTestBase<double?>
    {
        protected override double? GetResult(IQueryable<InventorySelect> source)
        {
            return source.Average(i => (double?)i.Quantity);
        }
    }
}
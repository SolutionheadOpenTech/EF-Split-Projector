using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Sum_SelectNullableDouble : LINQSingularMethodTestBase<double?>
    {
        protected override double? GetResult(IQueryable<InventorySelect> source)
        {
            return source.Sum(i => (double?)i.Quantity);
        }
    }
}
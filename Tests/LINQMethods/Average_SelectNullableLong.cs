using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Average_SelectNullableLong : LINQSingularMethodTestBase<double?>
    {
        protected override double? GetResult(IQueryable<InventorySelect> source)
        {
            return source.Average(i => (long?)i.Quantity);
        }
    }
}
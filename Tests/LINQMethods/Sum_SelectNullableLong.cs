using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Sum_SelectNullableLong : LINQSingularMethodTestBase<long?>
    {
        protected override long? GetResult(IQueryable<InventorySelect> source)
        {
            return source.Sum(i => (long?)i.Quantity);
        }
    }
}
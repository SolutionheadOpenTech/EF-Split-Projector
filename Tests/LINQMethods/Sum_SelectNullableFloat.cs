using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Sum_SelectNullableFloat : LINQSingularMethodTestBase<float?>
    {
        protected override float? GetResult(IQueryable<InventorySelect> source)
        {
            return source.Sum(i => (float?)i.Quantity);
        }
    }
}
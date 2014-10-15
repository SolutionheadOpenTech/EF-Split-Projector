using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Sum_Float : LINQSingularMethodTestBase<float>
    {
        protected override float GetResult(IQueryable<InventorySelect> source)
        {
            return source.Select(i => (float)i.Quantity).Sum();
        }
    }
}
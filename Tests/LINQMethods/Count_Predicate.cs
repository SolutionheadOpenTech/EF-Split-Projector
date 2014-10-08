using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Count_Predicate : LINQSingularMethodTestBase<int>
    {
        protected override int GetResult(IQueryable<InventorySelect> source)
        {
            return source.Count(i => i.Quantity > 0);
        }
    }
}
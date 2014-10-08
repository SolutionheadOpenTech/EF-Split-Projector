using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Count : LINQSingularMethodTestBase<int>
    {
        protected override int GetResult(IQueryable<InventorySelect> source)
        {
            return source.Count();
        }
    }
}
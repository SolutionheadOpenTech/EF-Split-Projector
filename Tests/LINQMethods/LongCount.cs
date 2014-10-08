using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class LongCount : LINQSingularMethodTestBase<long>
    {
        protected override long GetResult(IQueryable<InventorySelect> source)
        {
            return source.LongCount();
        }
    }
}
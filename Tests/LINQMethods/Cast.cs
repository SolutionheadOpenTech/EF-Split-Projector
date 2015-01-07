using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Cast : LINQQueryableInventoryMethodTestBase<object>
    {
        protected override IQueryable<object> GetQuery(IQueryable<InventorySelect> source)
        {
            return source.Cast<object>();
        }
    }
}
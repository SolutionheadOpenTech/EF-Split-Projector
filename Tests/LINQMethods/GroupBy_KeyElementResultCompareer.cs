using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class GroupBy_KeyElementResultCompareer : LINQQueryableMethodTestBase<IEnumerable<int>>
    {
        protected override IQueryable<IEnumerable<int>> GetQuery(IQueryable<InventorySelect> source)
        {
            return source.GroupBy(i => i.Location, i => i.Quantity, (k, e) => e, new Join_KeyComparer.Comparer());
        }
    }
}
using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class GroupBy_ElementSelectKeyComparer : LINQQueryableInventoryMethodTestBase<IGrouping<string, int>>
    {
        protected override IQueryable<IGrouping<string, int>> GetQuery(IQueryable<InventorySelect> source)
        {
            return source.GroupBy(i => i.Location, i => i.Quantity, new Join_KeyComparer.Comparer());
        }
    }
}
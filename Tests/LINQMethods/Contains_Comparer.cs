using System.Linq;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Contains_Comparer : LINQSingularMethodTestBase<bool>
    {
        protected override bool GetResult(IQueryable<InventorySelect> source)
        {
            return source.Contains(null, new TestComparer<InventorySelect>());
        }
    }
}
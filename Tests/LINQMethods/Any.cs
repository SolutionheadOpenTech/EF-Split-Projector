using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Any : LINQSingularMethodTestBase<bool>
    {
        protected override bool GetResult(IQueryable<InventorySelect> source)
        {
            return source.Any();
        }
    }
}
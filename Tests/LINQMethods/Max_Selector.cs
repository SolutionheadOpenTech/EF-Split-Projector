using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Max_Selector : LINQSingularMethodTestBase<string>
    {
        protected override string GetResult(IQueryable<InventorySelect> source)
        {
            return source.Max(i => i.Location);
        }
    }
}
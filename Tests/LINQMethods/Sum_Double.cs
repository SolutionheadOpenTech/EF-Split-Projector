using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Sum_Double : LINQSingularMethodTestBase<double>
    {
        protected override double GetResult(IQueryable<InventorySelect> source)
        {
            return source.Select(i => (double)i.Quantity).Sum();
        }
    }
}
using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Average_Int : LINQSingularMethodTestBase<double>
    {
        protected override double GetResult(IQueryable<InventorySelect> source)
        {
            return source.Select(i => (int)i.Quantity).Average();
        }
    }
}
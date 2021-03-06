using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Average_Decimal : LINQSingularMethodTestBase<decimal>
    {
        protected override decimal GetResult(IQueryable<InventorySelect> source)
        {
            var selectDecimals = source.Select(i => (decimal) i.Quantity);
            return selectDecimals.Average();
        }
    }
}
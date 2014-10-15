using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Average_SelectNullableDecimal : LINQSingularMethodTestBase<decimal?>
    {
        protected override decimal? GetResult(IQueryable<InventorySelect> source)
        {
            return source.Select(i => new
                {
                    Inventory = i,
                    Quantity = (decimal?)i.Quantity
                }).Average(i => i.Quantity);
        }
    }
}
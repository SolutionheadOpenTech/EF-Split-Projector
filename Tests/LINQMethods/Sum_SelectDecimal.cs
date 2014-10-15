using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Sum_SelectDecimal : LINQSingularMethodTestBase<decimal>
    {
        protected override decimal GetResult(IQueryable<InventorySelect> source)
        {
            return source.Select(i => new
                {
                    Inventory = i,
                    Quantity = (decimal)i.Quantity
                }).Sum(i => i.Quantity);
        }
    }
}
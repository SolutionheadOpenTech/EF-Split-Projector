using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Zip : LINQQueryableMethodTestBase<IntegratedTestsBase.InventorySelect>
    {
        protected override IQueryable<InventorySelect> GetQuery(IQueryable<InventorySelect> source)
        {
            var concat = new List<InventorySelect>
                {
                    new InventorySelect()
                };
            return source.Zip(concat, (a, b) => new InventorySelect
                {
                    Quantity = a.Quantity,
                    Location = b.Location
                });
        }
    }
}
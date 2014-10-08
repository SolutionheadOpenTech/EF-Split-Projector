using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Union : LINQQueryableMethodTestBase<IntegratedTestsBase.InventorySelect>
    {
        protected override IQueryable<InventorySelect> GetQuery(IQueryable<InventorySelect> source)
        {
            var union = new List<InventorySelect>
                {
                    new InventorySelect()
                };
            return source.Union(union);
        }
    }
}
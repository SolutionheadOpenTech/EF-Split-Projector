using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Select_WithIndex : LINQQueryableMethodTestBase<Select_WithIndex.SelectReturn>
    {
        public class SelectReturn
        {
            public InventorySelect InventorySelect;
            public int Index;
        }

        protected override IQueryable<SelectReturn> GetQuery(IQueryable<InventorySelect> source)
        {
            return source.Select((i, n) => new SelectReturn
                {
                    InventorySelect = i,
                    Index = n
                });
        }
    }
}
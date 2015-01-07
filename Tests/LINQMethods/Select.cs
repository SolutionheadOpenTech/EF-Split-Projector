using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Select : LINQQueryableInventoryMethodTestBase<Select.SelectReturn>
    {
        public class SelectReturn
        {
            public InventorySelect InventorySelect;
        }

        protected override IQueryable<SelectReturn> GetQuery(IQueryable<InventorySelect> source)
        {
            return source.Select(i => new SelectReturn
                {
                    InventorySelect = i
                });
        }
    }
}
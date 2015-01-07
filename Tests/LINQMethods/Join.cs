using System.Linq;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Join : LINQQueryableInventoryMethodTestBase<JoinSelect>
    {
        protected override IQueryable<JoinSelect> GetQuery(IQueryable<InventorySelect> source)
        {
            var items = TestHelper.Context.Items;
            return source.Join(items.Select(i => new
                                    {
                                        Item = i,
                                        i.Description
                                    }),
                               s => s.ItemDescription,
                               i => i.Description,
                               (s, i) => new JoinSelect
                                   {
                                       InventorySelect = s,
                                       Item = i.Item
                                   });
        }
    }
}
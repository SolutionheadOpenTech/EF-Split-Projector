using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Join_KeyComparer : LINQQueryableMethodTestBase<JoinSelect>
    {
        protected override IQueryable<JoinSelect> GetQuery(IQueryable<InventorySelect> source)
        {
            var items = TestHelper.Context.Items;
            return source.Join(items,
                               s => s.ItemDescription,
                               i => i.Description,
                               (s, i) => new JoinSelect
                                   {
                                       InventorySelect = s,
                                       Item = i
                                   }, new Comparer());
        }

        public class Comparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return x == y;
            }

            public int GetHashCode(string obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
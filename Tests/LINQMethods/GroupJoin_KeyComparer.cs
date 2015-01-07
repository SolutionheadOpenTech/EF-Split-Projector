using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class GroupJoin_KeyComparer : LINQQueryableInventoryMethodTestBase<GroupJoinSelect>
    {
        protected override IQueryable<GroupJoinSelect> GetQuery(IQueryable<InventorySelect> source)
        {
            var items = TestHelper.Context.Items;
            return source.GroupJoin(items,
                                    s => s.ItemDescription,
                                    i => i.Description,
                                    (s, i) => new GroupJoinSelect
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
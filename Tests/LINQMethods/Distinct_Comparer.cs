using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Distinct_Comparer : LINQQueryableMethodTestBase<int>
    {
        protected override IQueryable<int> GetQuery(IQueryable<InventorySelect> source)
        {
            return source.Select(i => i.Quantity).Distinct(new Comparer());
        }

        public class Comparer : IEqualityComparer<int>
        {
            public bool Equals(int x, int y)
            {
                return x.Equals(y);
            }

            public int GetHashCode(int obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
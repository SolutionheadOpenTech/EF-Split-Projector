using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class SequenceEqual_Comparer : LINQSingularMethodTestBase<bool>
    {
        protected override bool GetResult(IQueryable<InventorySelect> source)
        {
            var other = new List<InventorySelect>();
            return source.SequenceEqual(other, new TestComparer<InventorySelect>());
        }
    }
}
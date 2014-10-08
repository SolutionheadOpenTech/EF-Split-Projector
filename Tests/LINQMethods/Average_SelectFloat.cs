using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class Average_SelectFloat : LINQSingularMethodTestBase<float>
    {
        protected override float GetResult(IQueryable<InventorySelect> source)
        {
            return source.Average(i => (float)i.Quantity);
        }
    }
}
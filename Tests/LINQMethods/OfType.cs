using System.Linq;
using NUnit.Framework;

namespace Tests.LINQMethods
{
    [TestFixture]
    public class OfType : LINQQueryableInventoryMethodTestBase<IntegratedTestsBase.IInventorySelect>
    {
        protected override IQueryable<IInventorySelect> GetQuery(IQueryable<InventorySelect> source)
        {
            return source.OfType<IInventorySelect>();
        }
    }
}
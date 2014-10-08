//using System.Linq;
//using NUnit.Framework;

//namespace Tests.LINQMethods
//{
//    [TestFixture]
//    public class Distinct : LINQQueryableMethodTestBase<int>
//    {
//        protected override IQueryable<int> GetQuery(IQueryable<InventorySelect> source)
//        {
//            return source.Select(i => i.Quantity).Distinct();
//        }
//    }
//}
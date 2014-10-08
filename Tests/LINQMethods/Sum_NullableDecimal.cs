//using System.Linq;
//using NUnit.Framework;

//namespace Tests.LINQMethods
//{
//    [TestFixture]
//    public class Sum_NullableDecimal : LINQSingularMethodTestBase<decimal?>
//    {
//        protected override decimal? GetResult(IQueryable<InventorySelect> source)
//        {
//            return source.Select(i => (decimal?)i.Quantity).Sum();
//        }
//    }
//}
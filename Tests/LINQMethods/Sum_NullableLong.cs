//using System.Linq;
//using NUnit.Framework;

//namespace Tests.LINQMethods
//{
//    [TestFixture]
//    public class Sum_NullableLong : LINQSingularMethodTestBase<long?>
//    {
//        protected override long? GetResult(IQueryable<InventorySelect> source)
//        {
//            return source.Select(i => (long?)i.Quantity).Sum();
//        }
//    }
//}
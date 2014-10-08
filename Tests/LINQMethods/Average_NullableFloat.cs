//using System.Linq;
//using NUnit.Framework;

//namespace Tests.LINQMethods
//{
//    [TestFixture]
//    public class Average_NullableFloat : LINQSingularMethodTestBase<float?>
//    {
//        protected override float? GetResult(IQueryable<InventorySelect> source)
//        {
//            return source.Select(i => (float?)i.Quantity).Average();
//        }
//    }
//}
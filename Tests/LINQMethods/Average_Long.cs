//using System.Linq;
//using NUnit.Framework;

//namespace Tests.LINQMethods
//{
//    [TestFixture]
//    public class Average_Long : LINQSingularMethodTestBase<double>
//    {
//        protected override double GetResult(IQueryable<InventorySelect> source)
//        {
//            return source.Select(i => (long)i.Quantity).Average();
//        }
//    }
//}
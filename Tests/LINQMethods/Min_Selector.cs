//using System.Linq;
//using NUnit.Framework;

//namespace Tests.LINQMethods
//{
//    [TestFixture]
//    public class Min_Selector : LINQSingularMethodTestBase<string>
//    {
//        protected override string GetResult(IQueryable<InventorySelect> source)
//        {
//            return source.Min(i => i.Location);
//        }
//    }
//}
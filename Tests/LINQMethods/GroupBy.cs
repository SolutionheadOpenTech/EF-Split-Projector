//using System.Linq;
//using NUnit.Framework;

//namespace Tests.LINQMethods
//{
//    [TestFixture]
//    public class GroupBy : LINQQueryableMethodTestBase<IGrouping<string, IntegratedTestsBase.InventorySelect>>
//    {
//        protected override IQueryable<IGrouping<string, InventorySelect>> GetQuery(IQueryable<InventorySelect> source)
//        {
//            return source.GroupBy(i => i.Location);
//        }
//    }
//}
//using System.Linq;
//using NUnit.Framework;
//using Tests.LINQMethods.Select;

//namespace Tests.LINQMethods
//{
//    [TestFixture]
//    public class Join : LINQQueryableMethodTestBase<JoinResult>
//    {
//        protected override IQueryable<JoinResult> GetQuery(IQueryable<InventorySelect> source)
//        {
//            var items = TestHelper.Context.Items;
//            return source.Join(items,
//                               s => s.ItemDescription,
//                               i => i.Description,
//                               (s, i) => new JoinResult
//                                   {
//                                       InventorySelect = s,
//                                       Item = i
//                                   });
//        }
//    }
//}
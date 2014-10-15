//using System.Linq;
//using NUnit.Framework;
//using Tests.Helpers;

//namespace Tests.LINQMethods
//{
//    [TestFixture]
//    public class Join : LINQQueryableMethodTestBase<JoinSelect>
//    {
//        protected override IQueryable<JoinSelect> GetQuery(IQueryable<InventorySelect> source)
//        {
//            var items = TestHelper.Context.Items;
//            return source.Join(items,
//                               s => s.ItemDescription,
//                               i => i.Description,
//                               (s, i) => new JoinSelect
//                                   {
//                                       InventorySelect = s,
//                                       Item = i
//                                   });
//        }
//    }
//}
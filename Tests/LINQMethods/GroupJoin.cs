//using System.Linq;
//using NUnit.Framework;
//using Tests.LINQMethods.Select;

//namespace Tests.LINQMethods
//{
//    [TestFixture]
//    public class GroupJoin : LINQQueryableMethodTestBase<GroupJoinSelect>
//    {
//        protected override IQueryable<GroupJoinSelect> GetQuery(IQueryable<InventorySelect> source)
//        {
//            var items = TestHelper.Context.Items;
//            return source.GroupJoin(items,
//                                    s => s.ItemDescription,
//                                    i => i.Description,
//                                    (s, i) => new GroupJoinSelect
//                                        {
//                                            InventorySelect = s,
//                                            Item = i
//                                        });
//        }
//    }
//}
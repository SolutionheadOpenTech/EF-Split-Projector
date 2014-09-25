using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal static class QueryableSplitterHelper
    {
        public static IEnumerable<IQueryable> Split(IQueryable source, int preferredMaxDepth)
        {
            return SplitExpression(source.GetObjectContext(), source.Expression, preferredMaxDepth).Select(e => source.Provider.CreateQuery(e));
        }

        public static IEnumerable<IQueryable<T>> Split<T>(IQueryable<T> source, int preferredMaxDepth)
        {
            return SplitExpression(source.GetObjectContext(), source.Expression, preferredMaxDepth).Select(e => source.Provider.CreateQuery<T>(e));
        }
        
        private static IEnumerable<Expression> SplitExpression(ObjectContext objectContext, Expression source, int preferredMaxDepth)
        {
            return ShatterOnMemberInitVisitor.ShatterExpression(source).MergeShards(objectContext, preferredMaxDepth);
        }
    }
}
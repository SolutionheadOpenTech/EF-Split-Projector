using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Extensions;
using EF_Split_Projector.Helpers.Visitors;

namespace EF_Split_Projector
{
    public static class SplitQueryableExtensions
    {
        public static IQueryable<TResult> SplitSelect<TSource, TResult>(this IQueryable<TSource> source, params Expression<Func<TSource, TResult>>[] projectors)
        {
            var objectQuery = source.GetObjectQuery();
            if(objectQuery == null)
            {
                throw new NotSupportedException("source query must be backed by ObjectQuery implementation.");
            }

            return new SplitQueryable<TSource, TResult, TResult>(objectQuery, projectors, null);
        }

        public static IQueryable<TResult> SplitSelect<TSource, TResult>(this IQueryable<TSource> source, IEnumerable<Expression<Func<TSource, TResult>>> projectors)
        {
            var objectQuery = source.GetObjectQuery();
            if(objectQuery == null)
            {
                throw new NotSupportedException("source query must be backed by ObjectQuery implementation.");
            }
            return new SplitQueryable<TSource, TResult, TResult>(objectQuery, projectors, null);
        }

        public static IQueryable<TResult> AutoSplitSelect<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> projector, int preferredMaxDepth = 2)
        {
            var objectQuery = source.GetObjectQuery();
            if(objectQuery == null)
            {
                throw new NotSupportedException("source query must be backed by ObjectQuery implementation.");
            }
            var projectors = ShatterOnMemberInitVisitor.ShatterExpression(projector).MergeShards(objectQuery.Context, preferredMaxDepth);
            return new SplitQueryable<TSource, TResult, TResult>(objectQuery, projectors, source.Select(projector));
        }
    }
}
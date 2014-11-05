using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Visitors;

namespace EF_Split_Projector
{
    public static class ProjectorMerge
    {
        public static Expression<Func<TSource, TDest>> Merge<TSource, TDest>(this IEnumerable<Expression<Func<TSource, TDest>>> projectors)
        {
            return projectors == null ? null : MemberInitMergerVisitor.MergeOnProjectors(projectors.ToArray());
        }

        public static Expression<Func<TSource, TDest>> Merge<TSource, TDest>(params Expression<Func<TSource, TDest>>[] projectors)
        {
            return MemberInitMergerVisitor.MergeOnProjectors(projectors);
        }
    }
}

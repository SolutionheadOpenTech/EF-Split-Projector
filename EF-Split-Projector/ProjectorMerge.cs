using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Visitors;

namespace EF_Split_Projector
{
    public static class ProjectorMerge
    {
        public static Expression<Func<TSource, TDest>> Merge<TSource, TDest>(this IEnumerable<Expression<Func<TSource, TDest>>> projectors)
        {
            return MemberInitMergerVisitor.MergeLambdasOnMemberInit(projectors);
        }
    }
}

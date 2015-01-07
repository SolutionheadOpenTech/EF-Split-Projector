using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EF_Split_Projector.Helpers.Extensions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class MinimalProjectorVisitor : ExpressionVisitor
    {
        internal static Expression<Func<TSource, TDest>> CreateMinimalProjector<TSource, TDest>(Expression<Func<TSource, TDest>> projector, HashSet<MemberInfo> includeMembers)
        {
            return (Expression<Func<TSource, TDest>>) new MinimalProjectorVisitor(includeMembers).Visit(projector);
        }

        private readonly HashSet<MemberInfo> _includeMembers;

        private MinimalProjectorVisitor(HashSet<MemberInfo> includeMembers)
        {
            _includeMembers = includeMembers;
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            var minimalBindings = node.Bindings.Where(b => _includeMembers.Any(m => b.Member.IsOrImplements(m))).ToList();
            return base.VisitMemberInit(Expression.MemberInit(node.NewExpression, minimalBindings));
        }
    }
}
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class GatherDistinctMemberInfosVisitor : ExpressionVisitor
    {
        internal static HashSet<MemberInfo> GetMemberInfos(Expression expression)
        {
            var visitor = new GatherDistinctMemberInfosVisitor();
            visitor.Visit(expression);
            return visitor._memberInfos;
        }

        private readonly HashSet<MemberInfo> _memberInfos = new HashSet<MemberInfo>();

        private GatherDistinctMemberInfosVisitor() { }

        protected override Expression VisitMember(MemberExpression node)
        {
            if(!_memberInfos.Contains(node.Member))
            {
                _memberInfos.Add(node.Member);
            }
            return base.VisitMember(node);
        }
    }
}
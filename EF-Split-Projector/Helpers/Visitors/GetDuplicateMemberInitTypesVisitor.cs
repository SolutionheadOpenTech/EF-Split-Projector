using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class GetDuplicateMemberInitTypesVisitor : ExpressionVisitor
    {
        public static HashSet<Type> GetDuplicateMemberInitTypes(Expression expression)
        {
            var result = new HashSet<Type>();
            var visitor = new GetDuplicateMemberInitTypesVisitor();
            visitor.Visit(expression);
            foreach(var duplicate in visitor._memberInitTypes.GroupBy(t => t)
                                            .Select(g => g.ToList())
                                            .Where(g => g.Count() > 1))
            {
                result.Add(duplicate.First());
            }
            return result;
        }

        private readonly List<Type> _memberInitTypes = new List<Type>();

        private GetDuplicateMemberInitTypesVisitor() { }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            _memberInitTypes.Add(node.Type);
            return base.VisitMemberInit(node);
        }
    }
}
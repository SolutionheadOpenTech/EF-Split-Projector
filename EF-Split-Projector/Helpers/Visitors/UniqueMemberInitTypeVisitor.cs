using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class UniqueMemberInitTypeVisitor : ExpressionVisitor
    {
        public static Expression MakeMemberInitTypeUnique(Expression expression, HashSet<MemberInfo> bindMembers)
        {
            return new UniqueMemberInitTypeVisitor(bindMembers).Visit(expression);
        }

        private readonly HashSet<MemberInfo> _bindMembers;

        private UniqueMemberInitTypeVisitor(HashSet<MemberInfo> bindMembers)
        {
            _bindMembers = bindMembers;
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            var visited = base.VisitMemberInit(node);

            var visitedMemberInit = visited as MemberInitExpression;
            if(visitedMemberInit != null)
            {
                visited = Expression.MemberInit(Expression.New(DerivedTypeBuilder.BuildDerivedType(visitedMemberInit.Type)),
                    visitedMemberInit.Bindings.Where(b => _bindMembers == null || _bindMembers.Contains(b.Member)));
            }

            return visited;
        }
    }
}
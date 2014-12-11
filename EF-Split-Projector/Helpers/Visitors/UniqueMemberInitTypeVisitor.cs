using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class UniqueMemberInitTypeVisitor : ExpressionVisitor
    {
        public static Expression MakeMemberInitTypeUnique(Expression expression)
        {
            return new UniqueMemberInitTypeVisitor().Visit(expression);
        }

        private UniqueMemberInitTypeVisitor() { }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            return Expression.MemberInit(Expression.New(DerivedTypeBuilder.BuildDerivedType(node.Type)), node.Bindings);
        }
    }
}
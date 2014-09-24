using System.Collections.Generic;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class ReplaceBindingsVisitor : ExpressionVisitor
    {
        public static Expression ReplaceBindings(Expression source, MemberInitExpression memberInit, IEnumerable<MemberBinding> bindingReplacements)
        {
            var visitor = new ReplaceBindingsVisitor(memberInit, bindingReplacements);
            return visitor.Visit(source);
        }

        private readonly MemberInitExpression _memberInit;
        private readonly MemberInitExpression _replacement;

        private ReplaceBindingsVisitor(MemberInitExpression memberInit, IEnumerable<MemberBinding> bindingReplacements)
        {
            _memberInit = memberInit;
            _replacement = Expression.MemberInit(memberInit.NewExpression, bindingReplacements);
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            return node == _memberInit ? _replacement : base.VisitMemberInit(node);
        }
    }
}
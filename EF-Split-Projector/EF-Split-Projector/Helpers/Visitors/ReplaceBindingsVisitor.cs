using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class ReplaceBindingsVisitor : ExpressionVisitor
    {
        public static Expression ReplaceBindings(Expression source, MemberInitExpression memberInit, IEnumerable<MemberBinding> bindingReplacements)
        {
            var visitor = new ReplaceBindingsVisitor(new Dictionary<MemberInitExpression, IEnumerable<MemberBinding>> { { memberInit, bindingReplacements } });
            return visitor.Visit(source);
        }

        public static Expression ReplaceBindings(Expression source, Dictionary<MemberInitExpression, IEnumerable<MemberBinding>> bindingReplacements)
        {
            var visitor = new ReplaceBindingsVisitor(bindingReplacements);
            return visitor.Visit(source);
        }

        private readonly Dictionary<MemberInitExpression, MemberInitExpression> _replacements;

        private ReplaceBindingsVisitor(Dictionary<MemberInitExpression, IEnumerable<MemberBinding>> bindingReplacements)
        {
            _replacements = bindingReplacements.ToDictionary(r => r.Key, r => Expression.MemberInit(r.Key.NewExpression, r.Value));
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            MemberInitExpression replacement;
            return base.VisitMemberInit(_replacements.TryGetValue(node, out replacement) ? replacement : node);
        }
    }
}
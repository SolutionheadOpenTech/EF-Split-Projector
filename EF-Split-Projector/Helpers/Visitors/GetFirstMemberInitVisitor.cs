using System;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class GetFirstMemberInitVisitor : ExpressionVisitor
    {
        public static MemberInitExpression Get(Type initType, Expression expression)
        {
            var visitor = new GetFirstMemberInitVisitor(initType);
            visitor.Visit(expression);
            return visitor._memberInit;
        }

        private GetFirstMemberInitVisitor(Type initType)
        {
            _initType = initType;
        }

        private readonly Type _initType;
        private MemberInitExpression _memberInit;
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if(_memberInit == null && (_initType == null || node.Type == _initType || _initType.IsAssignableFrom(node.Type)))
            {
                _memberInit = node;
            }
            return base.VisitMemberInit(node);
        }
    }
}
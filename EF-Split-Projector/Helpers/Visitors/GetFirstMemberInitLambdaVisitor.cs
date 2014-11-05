using System;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class GetFirstMemberInitLambdaVisitor : ExpressionVisitor
    {
        public static LambdaExpression Get(Type initType, Expression expression)
        {
            var visitor = new GetFirstMemberInitLambdaVisitor(initType);
            visitor.Visit(expression);
            return visitor._lambdaExpression;
        }

        private GetFirstMemberInitLambdaVisitor(Type initType)
        {
            _initType = initType;
        }

        private readonly Type _initType;
        private LambdaExpression _lambdaExpression;
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if(_lambdaExpression == null)
            {
                var memberInit = node.Body as MemberInitExpression;
                if(memberInit != null && (_initType == null || memberInit.Type == _initType))
                {
                    _lambdaExpression = node;
                    return node;
                }
            }

            return base.VisitLambda<T>(node);
        }
    }
}
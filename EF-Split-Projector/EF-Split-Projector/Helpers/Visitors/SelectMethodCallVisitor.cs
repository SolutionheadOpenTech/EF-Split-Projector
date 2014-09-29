using System;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class SelectMethodCallVisitor : ExpressionVisitor
    {
        public SelectMethodCallVisitor(Expression expression)
        {
            Visit(expression);
        }

        public bool Success { get { return SourceQuery != null && SelectLambda != null; } }
        public IQueryable SourceQuery;
        public LambdaExpression SelectLambda;

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if(node.Method.Name == "Select")
            {
                var arguments = node.Arguments.ToList();
                if(arguments.Count == 2)
                {
                    var methodCall = arguments[0] as MethodCallExpression;
                    if(methodCall != null)
                    {
                        var constantExpression = methodCall.Object as ConstantExpression;
                        if(constantExpression != null)
                        {
                            SourceQuery = constantExpression.Value as IQueryable;
                            if(SourceQuery != null)
                            {
                                var unary = arguments[1] as UnaryExpression;
                                if(unary != null)
                                {
                                    SelectLambda = unary.Operand as LambdaExpression;
                                }
                            }
                        }
                    }
                }
            }

            return base.VisitMethodCall(node);
        }
    }
}
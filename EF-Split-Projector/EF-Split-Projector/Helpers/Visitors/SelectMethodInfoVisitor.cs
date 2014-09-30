using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class SelectMethodInfoVisitor : ExpressionVisitor
    {
        public static SelectMethodInfo GetSelectMethodInfo(Expression expression)
        {
            var visitor = new SelectMethodInfoVisitor();
            visitor.Visit(expression);
            return visitor._selectMethodInfo;
        }

        private SelectMethodInfoVisitor() { }

        private SelectMethodInfo _selectMethodInfo;

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if((_selectMethodInfo == null || !_selectMethodInfo.Valid) && node.Method.Name == "Select")
            {
                _selectMethodInfo = new SelectMethodInfo(node.Arguments.ToList());
            }

            return base.VisitMethodCall(node);
        }

        public class SelectMethodInfo
        {
            public readonly bool Valid;
            public readonly IQueryable SourceQueryable;
            public readonly LambdaExpression SelectLambdaExpression;
            public readonly List<Type> SelectLambdaTypeArguments;

            public SelectMethodInfo(List<Expression> selectMethodArguments)
            {
                if(selectMethodArguments != null && selectMethodArguments.Count == 2)
                {
                    var methodCall = selectMethodArguments[0] as MethodCallExpression;
                    if(methodCall != null)
                    {
                        var constantExpression = methodCall.Object as ConstantExpression;
                        if(constantExpression != null)
                        {
                            SourceQueryable = constantExpression.Value as IQueryable;
                            if(SourceQueryable != null)
                            {
                                var unary = selectMethodArguments[1] as UnaryExpression;
                                if(unary != null)
                                {
                                    SelectLambdaExpression = unary.Operand as LambdaExpression;
                                    if(SelectLambdaExpression != null)
                                    {
                                        SelectLambdaTypeArguments = SelectLambdaExpression.Type.GetGenericArguments().ToList();
                                        if(SelectLambdaTypeArguments.Count == 2)
                                        {
                                            Valid = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
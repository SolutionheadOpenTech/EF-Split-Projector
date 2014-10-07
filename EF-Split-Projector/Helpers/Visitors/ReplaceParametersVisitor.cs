using System;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class ReplaceParametersVisitor : ExpressionVisitor
    {
        /// <summary>
        /// Replaces all ParameterExpressions in supplied expression with supplied parameter expression.
        /// </summary>
        public static Expression ReplaceParameters(Expression expression, ParameterExpression parameter)
        {
            return new ReplaceParametersVisitor(parameter).Visit(expression);
        }

        private readonly ParameterExpression _parameter;

        private ReplaceParametersVisitor(ParameterExpression parameter)
        {
            if(parameter == null) { throw new ArgumentNullException("parameter"); }
            _parameter = parameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return _parameter;
        }
    }
}
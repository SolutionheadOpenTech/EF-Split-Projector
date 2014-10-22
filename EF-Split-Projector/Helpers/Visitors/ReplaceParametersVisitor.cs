using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class ReplaceParametersVisitor : ExpressionVisitor
    {
        public static T ReplaceParameters<T>(T expression, Dictionary<ParameterExpression, ParameterExpression> parameterReplacements)
            where T : Expression
        {
            return (T) new ReplaceParametersVisitor(parameterReplacements).Visit(expression);
        }

        public static T MergeLambdaParameters<T>(T lambda, T parametersSource)
            where T : LambdaExpression
        {
            var oldParameters = lambda.Parameters.ToList();
            var newParameters = parametersSource.Parameters.ToList();

            var mapping = oldParameters.Zip(newParameters, (o, n) => new
                {
                    Old = o,
                    New = n
                }).ToDictionary(p => p.Old, p => p.New);
            return ReplaceParameters(lambda, mapping);
        }

        private readonly Dictionary<ParameterExpression, ParameterExpression> _parameterReplacements;

        private ReplaceParametersVisitor(Dictionary<ParameterExpression, ParameterExpression> parameterReplacements)
        {
            _parameterReplacements = parameterReplacements;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            ParameterExpression replacement;
            if(_parameterReplacements.TryGetValue(node, out replacement))
            {
                node = replacement;
            }
            return base.VisitParameter(node);
        }
    }
}
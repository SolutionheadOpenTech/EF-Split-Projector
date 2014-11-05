using System;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Extensions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class MutateProjectorSingleParameterVisitor : ExpressionVisitor
    {
        /// <summary>
        /// Returns a new projector with a new input type given a selector from the new input type to the original input type.
        /// </summary>
        internal static Expression<Func<TNewSource, TDest>> Mutate<TOldSource, TNewSource, TDest>(Expression<Func<TOldSource, TDest>> projector, Expression<Func<TNewSource, TOldSource>> sourceSelector)
        {
            if(!projector.IsProjector()) { throw new ArgumentException("Expression is not projector.", "projector"); }
            if(sourceSelector == null) { throw new ArgumentNullException("sourceSelector"); }

            return new MutateProjectorSingleParameterVisitor().MutateSource(projector, sourceSelector);
        }

        #region Private Parts

        private Expression _newMemberAccess;
        private ParameterExpression _oldParameter;

        private Expression<Func<TNewSource, TDest>> MutateSource<TOldSource, TNewSource, TDest>(Expression<Func<TOldSource, TDest>> projector, Expression<Func<TNewSource, TOldSource>> sourceSelector)
        {
            _oldParameter = projector.Parameters.Single();
            _newMemberAccess = sourceSelector.Body;

            var newBody = Visit(projector.Body);
            return Expression.Lambda<Func<TNewSource, TDest>>(newBody, sourceSelector.Parameters);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newMemberAccess : base.VisitParameter(node);
        }

        #endregion
    }
}
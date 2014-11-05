using System;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Extensions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class MutateProjectorReturnVisitor : ExpressionVisitor
    {
        /// <summary>
        /// Returns a new projector resulting in a different return type (which must be derived from the projector's original return type, or else be the same type).
        /// </summary>
        internal static Expression<Func<TSource, TDestDerived>> Mutate<TSource, TDestBase, TDestDerived>(Expression<Func<TSource, TDestBase>> projector)
            where TDestBase : new()
            where TDestDerived : TDestBase, new()
        {
            if(!projector.IsProjector()) { throw new ArgumentException("Expression is not projector.", "projector"); }

            return new MutateProjectorReturnVisitor().MutateReturn<TSource, TDestBase, TDestDerived>(projector);
        }

        #region Private Parts

        private Type _newType;
        private NewExpression _oldNewExpression;

        private Expression<Func<TSource, TDestDerived>> MutateReturn<TSource, TDestBase, TDestDerived>(Expression<Func<TSource, TDestBase>> expression)
            where TDestBase : new()
            where TDestDerived : TDestBase, new()
        {
            _newType = typeof(TDestDerived);

            if(typeof(TDestBase) == _newType)
            {
                return Expression.Lambda<Func<TSource, TDestDerived>>(expression.Body, expression.Parameters);
            }

            _oldNewExpression = ((MemberInitExpression)expression.Body).NewExpression;

            var visitedExpression = (LambdaExpression)Visit(expression);
            return Expression.Lambda<Func<TSource, TDestDerived>>(visitedExpression.Body, visitedExpression.Parameters);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            return node == _oldNewExpression ? Expression.New(_newType) : base.VisitNew(node);
        }

        #endregion
    }
}
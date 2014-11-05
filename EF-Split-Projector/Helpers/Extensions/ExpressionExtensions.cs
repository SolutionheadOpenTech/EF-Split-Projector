using System;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Extensions
{
    public static class ExpressionExtensions
    {
        internal static bool IsProjector<TSource, TDest>(this Expression<Func<TSource, TDest>> expression)
        {
            return expression != null && expression.NodeType == ExpressionType.Lambda && expression.Body.NodeType == ExpressionType.MemberInit;
        }
    }
}

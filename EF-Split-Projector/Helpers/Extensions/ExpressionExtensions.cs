using System;
using System.Linq.Expressions;
using LinqKit;

namespace EF_Split_Projector.Helpers.Extensions
{
    public static class ExpressionExtensions
    {
        public static T ExpandAll<T>(this T p) where T : Expression
        {
            if(p == null)
            {
                return null;
            }

            while(p.ToString().Contains(".Invoke"))
            {
                p = (T)p.Expand();
            }

            return p;
        }

        internal static bool IsProjector<TSource, TDest>(this Expression<Func<TSource, TDest>> expression)
        {
            return expression != null && expression.NodeType == ExpressionType.Lambda && expression.Body.NodeType == ExpressionType.MemberInit;
        }
    }
}

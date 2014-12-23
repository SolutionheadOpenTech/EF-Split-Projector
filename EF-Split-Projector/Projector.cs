using System;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Extensions;

namespace EF_Split_Projector
{
    public static class Projector<T0>
    {
        public static Expression<Func<T0, TResult>> To<TResult>(Expression<Func<T0, TResult>> p)
        {
            return p.ExpandAll();
        }
    }

    public static class Projector<T0, T1>
    {
        public static Expression<Func<T0, T1, TResult>> To<TResult>(Expression<Func<T0, T1, TResult>> p)
        {
            return p.ExpandAll();
        }
    }

    public static class Projector<T0, T1, T2>
    {
        public static Expression<Func<T0, T1, T2, TResult>> To<TResult>(Expression<Func<T0, T1, T2, TResult>> p)
        {
            return p.ExpandAll();
        }
    }

    public static class Projector<T0, T1, T2, T3>
    {
        public static Expression<Func<T0, T1, T2, T3, TResult>> To<TResult>(Expression<Func<T0, T1, T2, T3, TResult>> p)
        {
            return p.ExpandAll();
        }
    }
}
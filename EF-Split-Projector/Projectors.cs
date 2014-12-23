using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Extensions;

namespace EF_Split_Projector
{
    public class Projectors<T0, TResult> : List<Expression<Func<T0, TResult>>>
    {
        public new void Add(Expression<Func<T0, TResult>> projector)
        {
            base.Add(projector.ExpandAll());
        }

        public void Add(IEnumerable<Expression<Func<T0, TResult>>> projectors)
        {
            AddRange(projectors.Select(p => p.ExpandAll()));
        }

        public void Add<TS, TD>(IEnumerable<Expression<Func<TS, TD>>> projectors, Func<Expression<Func<TS, TD>>, Expression<Func<T0, TResult>>> translate)
        {
            if(translate == null) { throw new ArgumentNullException("translate"); }
            Add(projectors.Select(translate));
        }
    }

    public class Projectors<T0, T1, TResult> : List<Expression<Func<T0, T1, TResult>>>
    {
        public new void Add(Expression<Func<T0, T1, TResult>> projector)
        {
            base.Add(projector.ExpandAll());
        }

        public void Add(IEnumerable<Expression<Func<T0, T1, TResult>>> projectors)
        {
            AddRange(projectors.Select(p => p.ExpandAll()));
        }

        public void Add<TS, TD>(IEnumerable<Expression<Func<TS, TD>>> projectors, Func<Expression<Func<TS, TD>>, Expression<Func<T0, T1, TResult>>> translate)
        {
            if(translate == null) { throw new ArgumentNullException("translate"); }
            Add(projectors.Select(translate));
        }
    }

    public class Projectors<T0, T1, T2, TResult> : List<Expression<Func<T0, T1, T2, TResult>>>
    {
        public new void Add(Expression<Func<T0, T1, T2, TResult>> projector)
        {
            base.Add(projector.ExpandAll());
        }

        public void Add(IEnumerable<Expression<Func<T0, T1, T2, TResult>>> projectors)
        {
            AddRange(projectors.Select(p => p.ExpandAll()));
        }

        public void Add<TS, TD>(IEnumerable<Expression<Func<TS, TD>>> projectors, Func<Expression<Func<TS, TD>>, Expression<Func<T0, T1, T2, TResult>>> translate)
        {
            if(translate == null) { throw new ArgumentNullException("translate"); }
            Add(projectors.Select(translate));
        }
    }

    public class Projectors<T0, T1, T2, T3, TResult> : List<Expression<Func<T0, T1, T2, T3, TResult>>>
    {
        public new void Add(Expression<Func<T0, T1, T2, T3, TResult>> projector)
        {
            base.Add(projector.ExpandAll());
        }

        public void Add(IEnumerable<Expression<Func<T0, T1, T2, T3, TResult>>> projectors)
        {
            AddRange(projectors.Select(p => p.ExpandAll()));
        }

        public void Add<TS, TD>(IEnumerable<Expression<Func<TS, TD>>> projectors, Func<Expression<Func<TS, TD>>, Expression<Func<T0, T1, T2, T3, TResult>>> translate)
        {
            if(translate == null) { throw new ArgumentNullException("translate"); }
            Add(projectors.Select(translate));
        }
    }
}
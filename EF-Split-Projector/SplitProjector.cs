using System;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers;
using EF_Split_Projector.Helpers.Visitors;

namespace EF_Split_Projector
{
    internal class SplitProjector<TSource, TProjection, TResult>
    {
        public readonly Expression<Func<TSource, TProjection>> Projector;
        public readonly ObjectMerger Merger;
        private readonly SplitQueryable<TSource, TProjection, TResult> _splitQueryable;

        public SplitProjector(SplitQueryable<TSource, TProjection, TResult> splitQueryable, Expression<Func<TSource, TProjection>> projector, bool createMerger)
        {
            _splitQueryable = splitQueryable;
            Projector = projector;
            Merger = createMerger ? ObjectMerger.CreateMerger(projector) : null;
        }

        public override string ToString()
        {
            return Projector == null ? "Projector[null]" : Projector.ToString();
        }

        public IQueryable<TProjection> CreateProjectedQuery()
        {
            return OrderByKeysVisitor.InjectOrderByEntityKeys(_splitQueryable.InternalSource.Select(Projector));
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers;

namespace EF_Split_Projector
{
    internal class SplitQueryable<TSource, TProjection, TResult> : SplitQueryableBase, IOrderedQueryable<TResult>, IDbAsyncEnumerable<TResult>
    {
        #region IQueryable

        public override Expression Expression { get { return InternalQuery.Expression; } }
        public override Type ElementType { get { return typeof(TResult); } }
        public override IQueryProvider Provider { get { return InternalProvider; } }

        #endregion

        #region IEnumerable

        protected override IEnumerator InternalGetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            var batchQueryResults = BatchQueriesHelper.ExecuteBatchQueries(InternalProjectors.Select(p => p.CreateProjectedQuery()).ToArray());
            var mergedResults = Merge<TProjection, TProjection>(batchQueryResults);
            return ((IEnumerable<TResult>) ExecutePending(mergedResults)).GetEnumerator();
        }

        #endregion

        #region IDbAsyncEnumerable

        protected override IDbAsyncEnumerator InternalGetAsyncEnumerator()
        {
            return GetAsyncEnumerator();
        }

        public IDbAsyncEnumerator<TResult> GetAsyncEnumerator()
        {
            return new SplitQueryDbAsyncEnumerator<TSource, TProjection, TResult>(this);
        }

        #endregion

        #region Private Internal

        internal readonly List<SplitProjector<TSource, TProjection, TResult>> InternalProjectors;
        internal readonly Expression<Func<TSource, TProjection>> InternalProjection;
        internal readonly ObjectQuery<TSource> InternalSource;
        internal readonly SplitQueryProvider<TSource, TProjection, TResult> InternalProvider;

        internal SplitQueryable(ObjectQuery<TSource> internalSource, IEnumerable<Expression<Func<TSource, TProjection>>> projectors, IQueryable internalQuery, IEnumerable<Func<IQueryable, object>> pendingMethodCalls = null)
        {
            if(projectors == null)
            {
                throw new ArgumentNullException("projectors");
            }

            InternalProjectors = projectors.Select((p, i) => new SplitProjector<TSource, TProjection, TResult>(this, p, i > 0)).ToList();
            if(!InternalProjectors.Any())
            {
                throw new ArgumentException("projectors cannot be empty");
            }
            InternalProjection = InternalProjectors.Select(p => p.Projector).Merge();
            InternalSource = internalSource;
            _InternalQuery = internalQuery ?? InternalSource.Select(InternalProjection);
            _InternalDelegates = pendingMethodCalls == null ? new List<Func<IQueryable, object>>() : pendingMethodCalls.ToList();
            InternalProvider = new SplitQueryProvider<TSource, TProjection, TResult>(this);
        }

        internal IEnumerable ExecutePending(IEnumerable enumerable)
        {
            return (IEnumerable)InternalDelegates.Aggregate((object)enumerable.AsQueryable(), (s, m) => m((IQueryable)s));
        }

        internal IEnumerable<TDest> Merge<T, TDest>(IEnumerable<List<T>> source)
        {
            var results = source.Zip(InternalProjectors.Select(p => p.Merger), (r, m) =>
                {
                    var enumerator = ((IEnumerable<T>)r).GetEnumerator();
                    enumerator.Reset();
                    return new
                        {
                            Merger = m,
                            Enumerator = enumerator
                        };
                }).ToList();

            try
            {
                while(results.All(r => r.Enumerator.MoveNext()))
                {
                    var index = 0;
                    yield return results.Aggregate(default(TDest),
                                                   (s, c) =>
                                                       {
                                                           if(index++ == 0 || c.Merger == null)
                                                           {
                                                               return (TDest)(object)c.Enumerator.Current;
                                                           }
                                                           return (TDest)c.Merger.Merge(s, c.Enumerator.Current);
                                                       });
                }
            }
            finally
            {
                results.ForEach(r => r.Enumerator.Dispose());
            }
        }

        protected override string GetCommandString()
        {
            return BatchQueriesHelper.GetBatchQueriesCommandString(InternalProjectors.Select(p => p.CreateProjectedQuery()).ToArray());
        }

        #endregion
    }
}
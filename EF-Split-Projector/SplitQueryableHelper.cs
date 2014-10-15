using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers;
using EF_Split_Projector.Helpers.Extensions;
using EF_Split_Projector.Helpers.Visitors;

namespace EF_Split_Projector
{
    public static class SplitQueryableHelper
    {
        public static IQueryable<TResult> SplitSelect<TSource, TResult>(this IQueryable<TSource> source, params Expression<Func<TSource, TResult>>[] projectors)
        {
            var objectQuery = source.GetObjectQuery();
            if(objectQuery == null)
            {
                throw new NotSupportedException("source query must be backed by ObjectQuery implementation.");
            }

            return new SplitQueryable<TSource, TResult, TResult>(objectQuery, projectors, null);
        }

        public static IQueryable<TResult> SplitSelect<TSource, TResult>(this IQueryable<TSource> source, IEnumerable<Expression<Func<TSource, TResult>>> projectors)
        {
            var objectQuery = source.GetObjectQuery();
            if(objectQuery == null)
            {
                throw new NotSupportedException("source query must be backed by ObjectQuery implementation.");
            }
            return new SplitQueryable<TSource, TResult, TResult>(objectQuery, projectors, null);
        }

        public static IQueryable<TResult> AutoSplitSelect<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> projector, int preferredMaxDepth = 2)
        {
            var objectQuery = source.GetObjectQuery();
            if(objectQuery == null)
            {
                throw new NotSupportedException("source query must be backed by ObjectQuery implementation.");
            }
            var projectors = ShatterOnMemberInitVisitor.ShatterExpression(projector).MergeShards(objectQuery.Context, preferredMaxDepth);
            return new SplitQueryable<TSource, TResult, TResult>(objectQuery, projectors, source.Select(projector));
        }

        private class SplitQueryable<TSource, TProjection, TResult> : IOrderedQueryable<TResult>, IDbAsyncEnumerable<TResult>
        {
            public Expression Expression { get { return _internalQuery.Expression; } }
            public Type ElementType { get { return typeof(TResult); } }
            public IQueryProvider Provider { get { return _provider; } }

            private readonly SplitQueryProvider _provider;
            private readonly IQueryable _internalQuery;
            private readonly ObjectQuery<TSource> _sourceQuery;
            private readonly List<SplitProjector> _splitProjectors;
            private readonly List<Func<IEnumerable, object>> _pendingDelegates;

            public SplitQueryable(ObjectQuery<TSource> sourceQuery, IEnumerable<Expression<Func<TSource, TProjection>>> projectors, IQueryable internalQuery, IEnumerable<Func<IEnumerable, object>> pendingMethodCalls = null)
            {
                if(projectors == null) { throw new ArgumentNullException("projectors"); }

                _splitProjectors = projectors.Select((p, i) => new SplitProjector(this, p, i > 0)).ToList();
                if(!_splitProjectors.Any())
                {
                    throw new ArgumentException("projectors cannot be empty");
                }

                _sourceQuery = sourceQuery;
                _provider = new SplitQueryProvider(this);
                _internalQuery = internalQuery ?? _sourceQuery.Select(MemberInitMerger.MergeMemberInits(_splitProjectors.Select(p => p.Projector).ToArray()));
                _pendingDelegates = pendingMethodCalls == null ? new List<Func<IEnumerable, object>>() : pendingMethodCalls.ToList();
            }

            private IEnumerable GetEnumerable()
            {
                var batchQueryResults = BatchQueriesHelper.ExecuteBatchQueries(_splitProjectors.Select(p => p.CreateProjectedQuery()).ToArray());
                var mergedResults = Merge<TProjection, TProjection>(batchQueryResults);
                return ExecutePending(mergedResults);
            }

            public IEnumerator<TResult> GetEnumerator()
            {
                var enumerable = ((IEnumerable<TResult>) GetEnumerable());
                return enumerable.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IDbAsyncEnumerator<TResult> GetAsyncEnumerator()
            {
                return new SplitQueryDbAsyncEnumerator(this);
            }

            IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator()
            {
                return GetAsyncEnumerator();
            }

            private IEnumerable ExecutePending(IEnumerable enumerable)
            {
                return (IEnumerable) _pendingDelegates.Aggregate((object)enumerable.AsQueryable(), (s, m) => m((IEnumerable)s));
            }

            private IEnumerable<TDest> Merge<T, TDest>(IEnumerable<List<T>> source)
            {
                var results = source.Zip(_splitProjectors.Select(p => p.Merger), (r, m) =>
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

            private class SplitQueryProvider : IDbAsyncQueryProvider
            {
                private readonly SplitQueryable<TSource, TProjection, TResult> _splitQueryable;

                public SplitQueryProvider(SplitQueryable<TSource, TProjection, TResult> splitQueryable)
                {
                    _splitQueryable = splitQueryable;
                }

                public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
                {
                    var newInternalQuery = _splitQueryable._internalQuery.Provider.CreateQuery<TElement>(expression);
                    var methodCall = expression as MethodCallExpression;
                    if(methodCall != null)
                    {
                        var newResultType = typeof(TElement);
                        if(newResultType.IsOrImplementsType<TProjection>() && !_splitQueryable._pendingDelegates.Any())
                        {
                            var projectors = _splitQueryable._splitProjectors.Select(q => q.Projector).ToArray();
                            var sourceQuery = (IQueryable) _splitQueryable._sourceQuery;
                            var translatedArguments = methodCall.Arguments.Select((a, i) => i == 0 ? sourceQuery.Expression : TranslateExpressionVisitor.TranslateFromProjectors(a, projectors)).ToArray();

                            var genericMethodDefinition = methodCall.Method.GetGenericMethodDefinition();
                            var typeArguments = methodCall.Method.GetGenericArguments().Select(a => a.ReplaceType(newResultType, typeof(TSource))).ToArray();
                            var methodInfo = genericMethodDefinition.MakeGenericMethod(typeArguments);
                            
                            var newSourceQuery = sourceQuery.Provider.CreateQuery<TSource>(Expression.Call(null, methodInfo, translatedArguments));

                            return (IQueryable<TElement>)new SplitQueryable<TSource, TProjection, TResult>(newSourceQuery.GetObjectQuery(), _splitQueryable._splitProjectors.Select(q => q.Projector), newInternalQuery);
                        }

                        var pending = _splitQueryable._pendingDelegates.ToList();
                        pending.Add(EnumerableMethodHelper.ConvertToDelegate(methodCall));
                        return new SplitQueryable<TSource, TProjection, TElement>(_splitQueryable._sourceQuery.AsQueryable().GetObjectQuery(), _splitQueryable._splitProjectors.Select(q => q.Projector), newInternalQuery, pending);
                    }

                    throw new NotSupportedException(string.Format("The expression '{0}' is not supported.", expression));
                }

                public TExecute Execute<TExecute>(Expression expression)
                {
                    if(typeof(TExecute).IsOrImplementsType<TProjection>())
                    {
                        var methodCall = expression as MethodCallExpression;
                        if(methodCall != null)
                        {
                            switch(methodCall.Arguments.Count)
                            {
                                    // At this point we're expecting that a method returning a singular result with a predicate parameter has been called (such as Single(x => ...), First(x => ...), etc).
                                    // Normally we would not be able to handle this by translating the predicate from the return type context to the data model type context as these methods
                                    // don't actually call into the Provider's CreateQuery method, and executing the expression off of the internal query will break if the predicate references
                                    // a property that is not being projected.
                                    // Instead, we inject a call to a Where clause passing in the supplied predicate which will then get translated into the data model type context as a a new query
                                    // and call the singular result method variant which has no predicate (assuming it exists).
                                    // RI - 2014/08/20
                                case 2:
                                    var expressionArgument = methodCall.Arguments[1] as UnaryExpression;
                                    var predicateType = typeof(Expression<Func<TExecute, bool>>);
                                    if(expressionArgument != null && expressionArgument.Type == predicateType)
                                    {
                                        var methodSansPredicate = QueryableHelper<TExecute>.GetMethod(methodCall.Method.Name, null);
                                        if(methodSansPredicate != null)
                                        {
                                            var whereMethod = QueryableHelper<TExecute>.GetMethod("Where", null, predicateType);
                                            if(whereMethod != null)
                                            {
                                                var newSplitQueryable = whereMethod.Invoke(null, new object[] { _splitQueryable, expressionArgument.Operand });
                                                if(newSplitQueryable != null)
                                                {
                                                    return (TExecute)methodSansPredicate.Invoke(null, new[] { newSplitQueryable });
                                                }
                                            }
                                        }
                                    }
                                    break;

                                    // At this point we're expecting that a method returning a singular result with no predicate parameter has been called (such as Single(x => ...), First(x => ...), etc)
                                    // (Possible we've ended up here after the above case).
                                    // In order to batch this query we use an internal method off of the assumed ObjectQueryProvider provider of the split projectors' CreateProjectedQuery result,
                                    // which will take the method call expression that returns a singular result and return a query representing the result as a set. The result sets are then merged
                                    // and the original extension method is called off of it.
                                    // RI - 2014/10/14
                                case 1:
                                    var batchResults = BatchQueriesHelper.ExecuteBatchQueries(methodCall, _splitQueryable._splitProjectors.Select(s => s.CreateProjectedQuery().GetObjectQuery()));
                                    var mergedResults = (IEnumerable<TExecute>)_splitQueryable.ExecutePending(_splitQueryable.Merge<TProjection, TProjection>(batchResults));

                                    switch(methodCall.Method.Name)
                                    {
                                        case "First": return mergedResults.First();
                                        case "FirstOrDefault": return mergedResults.FirstOrDefault();
                                        case "Single": return mergedResults.Single();
                                        case "SingleOrDefault": return mergedResults.SingleOrDefault();
                                    }
                                    break;
                            }
                        }
                    }

                    // All other method calls are forwarded to the internal query by default. This should work for calls to Any() or Count() and possibly anything else that is returning a primitive type
                    // instead of the queryable's return type - but that hasn't been thoroughly tested yet.
                    // RI - 2014/08/20
                    return _splitQueryable._internalQuery.Provider.Execute<TExecute>(expression);
                }

                public IQueryable CreateQuery(Expression expression)
                {
                    return CreateQuery<TProjection>(expression);
                }

                public object Execute(Expression expression)
                {
                    return Execute<TProjection>(expression);
                }

                public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
                {
                    return Task.FromResult(Execute(expression));
                }

                public Task<T> ExecuteAsync<T>(Expression expression, CancellationToken cancellationToken)
                {
                    return Task.FromResult(Execute<T>(expression));
                }
            }

            private class SplitQueryDbAsyncEnumerator : IDbAsyncEnumerator<TResult>
            {
                public SplitQueryDbAsyncEnumerator(SplitQueryable<TSource, TProjection, TResult> source)
                {
                    _source = source;
                }

                public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
                {
                    return Task.Run(() => GetEnumerator().MoveNext(), cancellationToken);
                }

                public TResult Current { get { return GetEnumerator().Current; } }

                object IDbAsyncEnumerator.Current { get { return Current; } }

                public void Dispose() { }

                private readonly SplitQueryable<TSource, TProjection, TResult> _source;
                private IEnumerator<TResult> GetEnumerator() { return (_enumerator ?? (_enumerator = _source.GetEnumerator())); }
                private IEnumerator<TResult> _enumerator;
            }

            private class SplitProjector
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
                    return OrderByKeysVisitor.InjectOrderByEntityKeys(_splitQueryable._sourceQuery.Select(Projector));
                }
            }
        }
    }
}
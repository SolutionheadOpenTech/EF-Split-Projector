using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EF_Split_Projector.Helpers;
using EF_Split_Projector.Helpers.Extensions;
using EF_Split_Projector.Helpers.Visitors;
using EntityFramework.Extensions;
using EntityFramework.Future;

namespace EF_Split_Projector
{
    public static class SplitQueryableHelper
    {
        public static IQueryable<TResult> SplitSelect<TSource, TResult>(this IQueryable<TSource> source, params Expression<Func<TSource, TResult>>[] projectors)
        {
            return new SplitQueryable<TSource, TResult>(source, projectors);
        }

        public static IQueryable<TResult> SplitSelect<TSource, TResult>(this IQueryable<TSource> source, IEnumerable<Expression<Func<TSource, TResult>>> projectors)
        {
            return new SplitQueryable<TSource, TResult>(source, projectors);
        }

        public static IQueryable<TResult> SplitSelect<TResult>(this IQueryable<TResult> source, int preferredMaxDepth = 2)
        {
            var shards = QueryableSplitterHelper.Split(source, preferredMaxDepth);
            if(shards != null)
            {
                var shardsList = shards.ToList();
                if(shardsList.Any())
                {
                    var visitors = shardsList.Select(s => new SelectMethodCallVisitor(s.Expression)).ToList();
                    if(visitors.All(v => v.Success))
                    {
                        var first = visitors.First();
                        var projectorTypes = first.SelectLambda.Type.GetGenericArguments().ToList();
                        var splitType = typeof(SplitQueryable<,>).MakeGenericType(projectorTypes.ToArray());
                        try
                        {
                            return (IQueryable<TResult>) Activator.CreateInstance(splitType, first.SourceQuery, visitors.Select(v => v.SelectLambda));
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
            }

            return source;
        }

        private class SplitQueryable<TSource, TResult> : IOrderedQueryable<TResult>, IDbAsyncEnumerable<TResult>
        {
            public Expression Expression { get { return _internalQuery.Expression; } }
            public Type ElementType { get { return typeof(TResult); } }
            public IQueryProvider Provider { get; private set; }

            private readonly IQueryable _internalQuery;
            private readonly IQueryable<TSource> _sourceQuery;
            private readonly List<SplitProjector> _splitProjectors;

            public SplitQueryable(IQueryable<TSource> sourceQuery, IEnumerable<Expression<Func<TSource, TResult>>> projectors, IQueryable internalQuery = null)
            {
                if(projectors == null) { throw new ArgumentNullException("projectors"); }

                _splitProjectors = projectors.Select((p, i) => new SplitProjector(this, p, i > 0)).ToList();
                if(!_splitProjectors.Any())
                {
                    throw new ArgumentException("projectors cannot be empty");
                }

                _sourceQuery = sourceQuery;
                Provider = new SplitQueryProvider(this);
                _internalQuery = internalQuery ?? _sourceQuery.Select(_splitProjectors.First().Projector);
            }

            public SplitQueryable(IQueryable<TSource> sourceQuery, IEnumerable<LambdaExpression> projectors)
                : this(sourceQuery, projectors.Select(p => (Expression<Func<TSource, TResult>>)p))
            { }

            public IEnumerator<TResult> GetEnumerator()
            {
                return Provider.Execute<IEnumerable<TResult>>(null).GetEnumerator();
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

            private class SplitQueryProvider : IDbAsyncQueryProvider
            {
                private readonly SplitQueryable<TSource, TResult> _splitQueryable;

                public SplitQueryProvider(SplitQueryable<TSource, TResult> splitQueryable)
                {
                    _splitQueryable = splitQueryable;
                }

                public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
                {
                    var newInternalQuery = _splitQueryable._internalQuery.Provider.CreateQuery<TElement>(expression);
                    var methodCall = expression as MethodCallExpression;
                    if(methodCall != null)
                    {
                        if(methodCall.Arguments.Count != 2)
                        {
                            throw new NotSupportedException("Expected 2 arguments for method call expression.");
                        }

                        var dataModelExpression = TranslateExpressionVisitor.TranslateFromProjectors(methodCall.Arguments[1], _splitQueryable._splitProjectors.Select(q => q.Projector).ToArray());
                        if(dataModelExpression != null)
                        {
                            var sourceType = typeof(TSource);
                            var newResultType = typeof(TElement);

                            var genericMethodDefinition = methodCall.Method.GetGenericMethodDefinition();
                            var typeArguments = methodCall.Method.GetGenericArguments().Select(a => a.IsOrImplementsType(newResultType) ? sourceType : a).ToArray();
                            var methodInfo = genericMethodDefinition.MakeGenericMethod(typeArguments);

                            var sourceQuery = _splitQueryable._sourceQuery;
                            var newSourceQuery = sourceQuery.Provider.CreateQuery<TSource>(Expression.Call(null, methodInfo, new[] { sourceQuery.Expression, dataModelExpression }));

                            return (IQueryable<TElement>)new SplitQueryable<TSource, TResult>(newSourceQuery, _splitQueryable._splitProjectors.Select(q => q.Projector), newInternalQuery);
                        }
                    }

                    throw new NotSupportedException(string.Format("The expression '{0}' is not supported.", expression));
                }

                public TExecute Execute<TExecute>(Expression expression)
                {
                    var executeType = typeof(TExecute);
                    if(executeType == typeof(IEnumerable<TResult>))
                    {
                        return (TExecute)GetEnumerable();
                    }

                    if(executeType.IsOrImplementsType<TResult>())
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
                                // Instead, we inject a call to a Where clause passing in the supplied predicate which will then get tranlsated into the data model type context as a a new query
                                // and call the singular result method variant which has no predicate (assuming it exists).
                                // RI - 2014/08/20
                                case 2:
                                    var expressionArgument = methodCall.Arguments[1] as UnaryExpression;
                                    var predicateType = typeof(Expression<Func<TExecute, bool>>);
                                    if(expressionArgument != null && expressionArgument.Type == predicateType)
                                    {
                                        var equivalentMethod = QueryableHelper<TExecute>.GetMethod(methodCall.Method.Name, null);
                                        if(equivalentMethod != null)
                                        {
                                            var whereMethod = QueryableHelper<TExecute>.GetMethod("Where", null, predicateType);
                                            if(whereMethod != null)
                                            {
                                                var newSplitQueryable = whereMethod.Invoke(null, new object[] { _splitQueryable, expressionArgument.Operand });
                                                if(newSplitQueryable != null)
                                                {
                                                    return (TExecute)equivalentMethod.Invoke(null, new[] { newSplitQueryable });
                                                }
                                            }
                                        }
                                    }
                                    break;

                                // At this point we're expecting that a method returning a singular result with no predicate parameter has been called (such as Single(x => ...), First(x => ...), etc).
                                // Currently, we're translating the call to the underlying queryable expression in the splitQueryables, executing them one at a time (taking multiple database hits), and
                                // merging the results into a single object.
                                // RI - 2014/08/20
                                case 1:
                                    var result = (TExecute)_splitQueryable._splitProjectors[0].ExecuteQuery(methodCall);
                                    foreach(var query in _splitQueryable._splitProjectors.Where((s, i) => i > 0))
                                    {
                                        var split = query.ExecuteQuery(methodCall);
                                        result = query.MergeResults<TExecute>(result, split);
                                    }
                                    return result;
                            }

                            throw new NotSupportedException(String.Format("The expression {0} is not supported.", expression));
                        }
                    }

                    // All other method calls are forwarded to the internal query by default - this should work for calls to Any() or Count() and possibly anything else that is returning a primitive type
                    // instead of the queryable's return type, but that hasn't been thoroughly tested yet.
                    // RI - 2014/08/20
                    return _splitQueryable._internalQuery.Provider.Execute<TExecute>(expression);
                }

                public IQueryable CreateQuery(Expression expression)
                {
                    return CreateQuery<TResult>(expression);
                }

                public object Execute(Expression expression)
                {
                    return Execute<TResult>(expression);
                }

                private IEnumerable<TResult> GetEnumerable()
                {
                    try
                    {
                        _splitQueryable._splitProjectors.ForEach(s => s.PrimeForFuture());

                        TResult result;
                        while(GetNextResult(out result))
                        {
                            yield return result;
                        }
                    }
                    finally
                    {
                        _splitQueryable._splitProjectors.ForEach(m => m.Dispose());
                    }
                }

                private bool GetNextResult(out TResult result)
                {
                    result = default(TResult);
                    foreach(var query in _splitQueryable._splitProjectors)
                    {
                        TResult nextResult;
                        if(!query.MoveNext(out nextResult))
                        {
                            return false;
                        }

                        result = query.MergeResults<TResult>(result, nextResult);
                    }
                    return true;
                }

                public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
                {
                    return Task.FromResult(Execute(expression));
                }

                public Task<TResult1> ExecuteAsync<TResult1>(Expression expression, CancellationToken cancellationToken)
                {
                    return Task.FromResult(Execute<TResult1>(expression));
                }
            }

            private class SplitQueryDbAsyncEnumerator : IDbAsyncEnumerator<TResult>
            {
                public SplitQueryDbAsyncEnumerator(SplitQueryable<TSource, TResult> source)
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

                private readonly SplitQueryable<TSource, TResult> _source;
                private IEnumerator<TResult> GetEnumerator() { return (_enumerator ?? (_enumerator = _source.GetEnumerator())); }
                private IEnumerator<TResult> _enumerator;
            }

            private class SplitProjector
            {
                public readonly Expression<Func<TSource, TResult>> Projector;
                private readonly SplitQueryable<TSource, TResult> _splitQueryable;
                private readonly ObjectMerger _merger;

                private FutureQuery<TResult> _future;
                private IEnumerator<TResult> _enumerator;

                public SplitProjector(SplitQueryable<TSource, TResult> splitQueryable, Expression<Func<TSource, TResult>> projector, bool createMerger)
                {
                    _splitQueryable = splitQueryable;
                    Projector = projector;
                    _merger = createMerger ? ObjectMerger.CreateMerger(projector) : null;
                }

                public object ExecuteQuery(MethodCallExpression methodCall)
                {
                    var query = CreateProjectedQuery();
                    return query.Provider.Execute(Expression.Call(null, methodCall.Method.GetGenericMethodDefinition().MakeGenericMethod(typeof(TResult)), new[] { query.Expression }));
                }

                public void PrimeForFuture()
                {
                    _future = PrivateHelperMethods.CreateFutureQuery(CreateProjectedQuery());
                }

                public bool MoveNext(out TResult result)
                {
                    result = default(TResult);

                    if(_enumerator == null)
                    {
                        _enumerator = _future != null ? _future.GetEnumerator() : CreateProjectedQuery().GetEnumerator();
                    }

                    if(_enumerator.MoveNext())
                    {
                        result = _enumerator.Current;
                        return true;
                    }

                    return false;
                }

                public TMergeResult MergeResults<TMergeResult>(object previousResult, object nextResult)
                {
                    if(_merger != null)
                    {
                        return (TMergeResult)_merger.Merge(previousResult, nextResult);
                    }

                    return (TMergeResult)(previousResult ?? nextResult);
                }

                public void Dispose()
                {
                    if(_enumerator != null)
                    {
                        _enumerator.Dispose();
                        _enumerator = null;
                        _future = null;
                    }
                }

                private IQueryable<TResult> CreateProjectedQuery()
                {
                    return OrderByKeysVisitor.InjectOrderByEntityKeys(_splitQueryable._sourceQuery.Select(Projector));
                }
            }
        }

        private static class PrivateHelperMethods
        {
            /// <summary>
            /// Returns a FutureQuery by bypassing TFuture : class constraint in signature - will likely fail if TFuture : class is not true.
            /// </summary>
            public static FutureQuery<TFuture> CreateFutureQuery<TFuture>(IQueryable<TFuture> source)
            {
                return (FutureQuery<TFuture>)CreateFutureQueryMethodInfo.MakeGenericMethod(typeof(TFuture)).Invoke(null, new object[] { source });
            }

            private static readonly MethodInfo CreateFutureQueryMethodInfo = typeof(PrivateHelperMethods).GetMethod("_CreateFutureQuery", BindingFlags.Static | BindingFlags.NonPublic);

            // ReSharper disable InconsistentNaming
            // ReSharper disable UnusedMember.Local
            /// <summary>
            /// Keep around for reflection purposes.
            /// </summary>
            private static FutureQuery<TFuture> _CreateFutureQuery<TFuture>(IQueryable<TFuture> source)
                where TFuture : class
            {
                return source.Future();
            }
            // ReSharper restore UnusedMember.Local
            // ReSharper restore InconsistentNaming
        }
    }
}
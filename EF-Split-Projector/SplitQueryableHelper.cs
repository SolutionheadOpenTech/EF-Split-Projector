using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Reflection;
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
            return new SplitQueryable<TSource, TResult>(source, projectors, null);
        }

        public static IQueryable<TResult> SplitSelect<TSource, TResult>(this IQueryable<TSource> source, IEnumerable<Expression<Func<TSource, TResult>>> projectors)
        {
            return new SplitQueryable<TSource, TResult>(source, projectors, null);
        }

        public static IQueryable<TResult> AsSplitQueryable<TResult>(this IQueryable<TResult> source, int preferredMaxDepth = 2)
        {
            var shards = QueryableSplitterHelper.Split(source, preferredMaxDepth);
            if(shards != null)
            {
                var visitors = shards.Select(s => SelectMethodInfoVisitor.GetSelectMethodInfo(s.Expression)).ToList();
                if(visitors.All(v => v.Valid))
                {
                    var first = visitors.First();
                    if(first.SelectLambdaTypeArguments.Count == 2)
                    {
                        var splitType = typeof(SplitQueryable<,>).MakeGenericType(first.SelectLambdaTypeArguments.ToArray());
                        return (IQueryable<TResult>) Activator.CreateInstance(splitType, first.SourceQueryable, visitors.Select(v => v.SelectLambdaExpression), source);
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

            public SplitQueryable(IQueryable<TSource> sourceQuery, IEnumerable<Expression<Func<TSource, TResult>>> projectors, IQueryable internalQuery)
            {
                if(projectors == null) { throw new ArgumentNullException("projectors"); }

                _splitProjectors = projectors.Select((p, i) => new SplitProjector(this, p, i > 0)).ToList();
                if(!_splitProjectors.Any())
                {
                    throw new ArgumentException("projectors cannot be empty");
                }

                _sourceQuery = sourceQuery;
                Provider = new SplitQueryProvider(this);

                _internalQuery = internalQuery ?? _sourceQuery.Select(MemberInitMerger.MergeMemberInits(_splitProjectors.Select(p => p.Projector).ToArray()));
            }

            // ReSharper disable UnusedMember.Local
            // Used by Activator.
            public SplitQueryable(IQueryable<TSource> sourceQuery, IEnumerable<LambdaExpression> projectors, IQueryable internalQuery)
                : this(sourceQuery, projectors.Select(p => (Expression<Func<TSource, TResult>>)p), internalQuery) { }
            // ReSharper restore UnusedMember.Local

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

                                // At this point we're expecting that a method returning a singular result with no predicate parameter has been called (such as Single(x => ...), First(x => ...), etc)
                                // (Possible we've ended up here after the above case).
                                // In order to batch this query we use an internal method off of the assumed ObjectQueryProvider provider of the split projectors' CreateProjectedQuery result,
                                // which will take the method call expression that returns a singular result and return a query representing the result as a set. The result sets are then merged
                                // and the original method call extension method is called off of it.
                                // RI - 2014/10/14
                                case 1:
                                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                                    var objectQueryInfo = typeof(ObjectQuery).GetProperty("ObjectQueryProvider", flags);
                                    var createQueryInfo = objectQueryInfo.PropertyType.GetMethods(flags)
                                        .Single(m => m.Name == "CreateQuery" && m.GetParameters().Count() == 1)
                                        .MakeGenericMethod(typeof(TExecute));

                                    var newQueries = _splitQueryable._splitProjectors.Select(s =>
                                        {
                                            var projectedQuery = s.CreateProjectedQuery();
                                            var objectQuery = projectedQuery.GetObjectQuery();
                                            var provider = objectQueryInfo.GetValue(objectQuery);
                                            var arguments = methodCall.Arguments.ToList();
                                            arguments[0] = projectedQuery.Expression;
                                            var splitExpression = Expression.Call(null, methodCall.Method, arguments);
                                            return (IQueryable<TResult>)createQueryInfo.Invoke(provider, new object[] { splitExpression });
                                        });

                                    var results = Merge<TResult, TExecute>(BatchQueriesHelper.ExecuteBatchQueries(newQueries.ToArray()));

                                    switch(methodCall.Method.Name)
                                    {
                                        case "First": return results.First();
                                        case "FirstOrDefault": return results.FirstOrDefault();
                                        case "Single": return results.Single();
                                        case "SingleOrDefault": return results.SingleOrDefault();
                                    }
                                    break;
                            }

                            throw new NotSupportedException(string.Format("The expression {0} is not supported.", expression));
                        }
                    }

                    // All other method calls are forwarded to the internal query by default. This should work for calls to Any() or Count() and possibly anything else that is returning a primitive type
                    // instead of the queryable's return type - but that hasn't been thoroughly tested yet.
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

                private static IEnumerator<T> ResetEnumerator<T>(IEnumerator<T> enumerator)
                {
                    enumerator.Reset();
                    return enumerator;
                }

                private IEnumerable<TResult> GetEnumerable()
                {
                    return Merge<TResult, TResult>(BatchQueriesHelper.ExecuteBatchQueries(_splitQueryable._splitProjectors.Select(p => p.CreateProjectedQuery()).ToArray()));
                }

                private IEnumerable<TDest> Merge<T, TDest>(IEnumerable<List<T>> source)
                {
                    var results = source.Select((r, i) => new
                        {
                            Projector = _splitQueryable._splitProjectors[i],
                            Enumerator = ResetEnumerator(r.GetEnumerator())
                        }).ToList();
                    try
                    {
                        while(results.All(r => r.Enumerator.MoveNext()))
                        {
                            var index = 0;
                            var result = results.Aggregate(default(TDest), (s, c) => index++ == 0 ? (TDest)(object)c.Enumerator.Current : c.Projector.MergeResults<TDest>(s, c.Enumerator.Current));
                            yield return result;
                        }
                    }
                    finally
                    {
                        results.ForEach(r => r.Enumerator.Dispose());
                    }
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

                public SplitProjector(SplitQueryable<TSource, TResult> splitQueryable, Expression<Func<TSource, TResult>> projector, bool createMerger)
                {
                    _splitQueryable = splitQueryable;
                    Projector = projector;
                    _merger = createMerger ? ObjectMerger.CreateMerger(projector) : null;
                }

                public TMergeResult MergeResults<TMergeResult>(object previousResult, object nextResult)
                {
                    if(_merger != null)
                    {
                        return (TMergeResult)_merger.Merge(previousResult, nextResult);
                    }

                    return (TMergeResult)(previousResult ?? nextResult);
                }

                public override string ToString()
                {
                    return Projector == null ? "Projector[null]" : Projector.ToString();
                }

                public IQueryable<TResult> CreateProjectedQuery()
                {
                    return OrderByKeysVisitor.InjectOrderByEntityKeys(_splitQueryable._sourceQuery.Select(Projector));
                }
            }
        }
    }
}
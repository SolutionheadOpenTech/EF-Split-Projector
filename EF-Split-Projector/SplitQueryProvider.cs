using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EF_Split_Projector.Helpers;
using EF_Split_Projector.Helpers.Extensions;
using EF_Split_Projector.Helpers.Visitors;

namespace EF_Split_Projector
{
    internal class SplitQueryProvider<TSource, TProjection, TResult> : IDbAsyncQueryProvider
    {
        #region IQueryProvider

        public IQueryable CreateQuery(Expression expression)
        {
            return CreateQuery<TProjection>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            var newInternalQuery = _splitQueryable.InternalQuery.Provider.CreateQuery<TElement>(expression);
            var methodCall = expression as MethodCallExpression;
            if(methodCall != null)
            {
                var newResultType = typeof(TElement);
                if(newResultType.IsOrImplementsType<TProjection>() && !_splitQueryable.InternalDelegates.Any())
                {
                    var sourceQuery = (IQueryable) _splitQueryable.InternalSource;
                    var translatedExpression = TranslateExpressionVisitor.TranslateMethodCall(methodCall, sourceQuery.Expression, _splitQueryable.InternalProjection);
                    var newSourceQuery = sourceQuery.Provider.CreateQuery<TSource>(translatedExpression);

                    return (IQueryable<TElement>) new SplitQueryable<TSource, TProjection, TResult>(newSourceQuery.GetObjectQuery(), _splitQueryable.InternalProjectors.Select(q => q.Projector), newInternalQuery);
                }

                var pending = _splitQueryable.InternalDelegates.ToList();
                pending.Add(EnumerableMethodHelper.ToCreateQueryDelegate(methodCall));
                return new SplitQueryable<TSource, TProjection, TElement>(_splitQueryable.InternalSource.AsQueryable().GetObjectQuery(), _splitQueryable.InternalProjectors.Select(q => q.Projector), newInternalQuery, pending);
            }

            throw new NotSupportedException(string.Format("The expression '{0}' is not supported.", expression));
        }

        public object Execute(Expression expression)
        {
            return Execute<TProjection>(expression);
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
                                var methodSansPredicate = QueryableMethodHelper<TExecute>.GetMethod(methodCall.Method.Name, null);
                                if(methodSansPredicate != null)
                                {
                                    var whereMethod = QueryableMethodHelper<TExecute>.GetMethod("Where", null, predicateType);
                                    if(whereMethod != null)
                                    {
                                        var newSplitQueryable = whereMethod.Invoke(null, new object[] { _splitQueryable, expressionArgument.Operand });
                                        if(newSplitQueryable != null)
                                        {
                                            return (TExecute) methodSansPredicate.Invoke(null, new[] { newSplitQueryable });
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
                            var batchResults = BatchQueriesHelper.ExecuteBatchQueries(methodCall, _splitQueryable.InternalProjectors.Select(s => s.CreateProjectedQuery().GetObjectQuery()));
                            var mergedResults = (IEnumerable<TExecute>) _splitQueryable.ExecutePending(_splitQueryable.Merge<TProjection, TProjection>(batchResults));

                            switch(methodCall.Method.Name)
                            {
                                case "First":
                                    return mergedResults.First();
                                case "FirstOrDefault":
                                    return mergedResults.FirstOrDefault();
                                case "Single":
                                    return mergedResults.Single();
                                case "SingleOrDefault":
                                    return mergedResults.SingleOrDefault();
                            }
                            break;
                    }
                }
            }

            // All other method calls are forwarded to the internal query by default. This should work for calls to Any() or Count() and possibly anything else that is returning a primitive type
            // instead of the queryable's return type - but that hasn't been thoroughly tested yet.
            // RI - 2014/08/20
            return _splitQueryable.InternalQuery.Provider.Execute<TExecute>(expression);
        }

        #endregion

        #region IDbAsyncQueryProvider

        public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute(expression));
        }

        public Task<T> ExecuteAsync<T>(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute<T>(expression));
        }

        #endregion

        #region Private Internal

        internal SplitQueryProvider(SplitQueryable<TSource, TProjection, TResult> splitQueryable)
        {
            _splitQueryable = splitQueryable;
        }

        private readonly SplitQueryable<TSource, TProjection, TResult> _splitQueryable;

        #endregion
    }
}
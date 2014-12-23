using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using EF_Split_Projector.Helpers.Extensions;

namespace EF_Split_Projector.Helpers
{
    internal static class BatchQueriesHelper
    {
        private const BindingFlags ReflectionBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly PropertyInfo ObjectQueryProviderInfo = typeof(ObjectQuery).GetProperty("ObjectQueryProvider", ReflectionBindingFlags);
        private static readonly MethodInfo CreateQueryDefinitionInfo = ObjectQueryProviderInfo.PropertyType.GetMethods(ReflectionBindingFlags).Single(m => m.Name == "CreateQuery" && m.GetParameters().Count() == 1);

        public static List<List<T>> ExecuteBatchQueries<T>(MethodCallExpression methodCallExpression, IEnumerable<ObjectQuery<T>> objectQueries)
        {
            var createQueryInfo = CreateQueryDefinitionInfo.MakeGenericMethod(typeof(T));
            var queries = objectQueries.Select(q =>
                {
                    var objectQuery = q.GetObjectQuery();
                    var arguments = methodCallExpression.Arguments.ToList();
                    arguments[0] = ((IQueryable) objectQuery).Expression;

                    return (IQueryable<T>) createQueryInfo.Invoke(ObjectQueryProviderInfo.GetValue(objectQuery),
                        new object[] { Expression.Call(null, methodCallExpression.Method, arguments) });
                });
            return ExecuteBatchQueries(queries.ToArray());
        }

        public static List<List<T>> ExecuteBatchQueries<T>(params IQueryable<T>[] queries)
        {
            Logging.Start("ExecuteBatchQueries");

            IEnumerable<ObjectQuery> objectQueries;
            var context = GetObjectQueriesContext(queries, out objectQueries);

            var contextType = context.GetType();
            contextType.GetMethod("EnsureConnection", ReflectionBindingFlags).Invoke(context, new object[] { false });

            List<List<T>> results;
            try
            {
                results = ResultsHelper.GetResults<T>(objectQueries, context);
            }
            finally
            {
                contextType.GetMethod("ReleaseConnection", ReflectionBindingFlags).Invoke(context, null);
            }

            Logging.Stop();
            return results;
        }

        public static string GetBatchQueriesCommandString<T>(params IQueryable<T>[] queries)
        {
            IEnumerable<ObjectQuery> objectQueries;
            var context = GetObjectQueriesContext(queries, out objectQueries);

            var contextType = context.GetType();
            contextType.GetMethod("EnsureConnection", ReflectionBindingFlags).Invoke(context, new object[] { false });

            string commandString;
            try
            {
                var indexStart = 1;
                using(var command = CreateBatchCommand(objectQueries, context, ref indexStart))
                {
                    commandString = command.CommandText;
                }
            }
            finally
            {
                contextType.GetMethod("ReleaseConnection", ReflectionBindingFlags).Invoke(context, null);
            }
            return commandString;
        }

        private static ObjectContext GetObjectQueriesContext<T>(IEnumerable<IQueryable<T>> queries, out IEnumerable<ObjectQuery> objectQueries)
        {
            objectQueries = queries.Select(q => q.GetObjectQuery()).ToList();
            var contexts = objectQueries.Select(q => q.Context).Distinct().ToList();
            if(contexts.Count != 1)
            {
                throw new NotSupportedException(string.Format("Expected queries with single distinct ObjectContext, but received {0} distinct ObjecContexts.", contexts.Count));
            }
            return contexts.Single();
        }

        private static DbCommand CreateBatchCommand(IEnumerable<ObjectQuery> queries, ObjectContext objectContext, ref int indexStart)
        {
            var dbConnection = objectContext.Connection;
            var entityConnection = dbConnection as EntityConnection;

            var batchCommand = entityConnection == null ? dbConnection.CreateCommand() : entityConnection.StoreConnection.CreateCommand();
            var batchSql = new StringBuilder();
            foreach(var query in queries)
            {                
                var commandText = query.ToTraceString();
                foreach(var parameter in query.Parameters)
                {
                    var updatedName = string.Format("f{0}_{1}", indexStart, parameter.Name);
                    commandText = commandText.Replace("@" + parameter.Name, "@" + updatedName);

                    var dbParameter = batchCommand.CreateParameter();
                    dbParameter.ParameterName = updatedName;
                    dbParameter.Value = parameter.Value ?? DBNull.Value;
                    batchCommand.Parameters.Add(dbParameter);
                }

                if(batchSql.Length > 0)
                {
                    batchSql.AppendLine();
                }

                batchSql.Append("-- Query #");
                batchSql.Append(indexStart++);
                batchSql.AppendLine();
                batchSql.AppendLine();
                batchSql.Append(commandText.Trim());
                batchSql.AppendLine(";");
            }

            batchCommand.CommandText = batchSql.ToString();
            if(objectContext.CommandTimeout.HasValue)
            {
                batchCommand.CommandTimeout = objectContext.CommandTimeout.Value;
            }

            return batchCommand;
        }

        private static class ResultsHelper
        {
            internal static List<List<T>> GetResults<T>(IEnumerable<ObjectQuery> objectQueries, ObjectContext context)
            {
                var indexStart = 1;
                var results = new List<List<T>>();

                if(EFSplitProjectorSection.Diagnostics == EFSplitProjectorSection.DiagnosticType.LoggingUnbatchedQueries)
                {
                    foreach(var query in objectQueries)
                    {
                        Logging.Start(string.Format("ExecutingQuery #{0}", indexStart));
                        using(var command = CreateBatchCommand(new [] { query }, context, ref indexStart))
                        using(var reader = command.ExecuteReader())
                        {
                            results.Add(ReadResults<T>(query, context, reader));
                        }
                        Logging.Stop();
                    }
                }
                else
                {
                    using(var command = CreateBatchCommand(objectQueries, context, ref indexStart))
                    using(var reader = command.ExecuteReader())
                    {
                        foreach(var query in objectQueries)
                        {
                            results.Add(ReadResults<T>(query, context, reader));
                            reader.NextResult();
                        }
                    }
                }

                return results;
            }
            
            private static List<T> ReadResults<T>(ObjectQuery query, ObjectContext context, DbDataReader reader)
            {
                var queryState = query.GetType().GetProperty("QueryState", ReflectionBindingFlags).GetValue(query);
                var executionPlan = queryState.GetType().GetMethod("GetExecutionPlan", ReflectionBindingFlags).Invoke(queryState, new object[] { null });
                var shaperFactory = executionPlan.GetType().GetField("ResultShaperFactory", ReflectionBindingFlags).GetValue(executionPlan);
                var shaper = shaperFactory.GetType().GetMethod("Create", ReflectionBindingFlags).Invoke(shaperFactory, new object[] { reader, context, context.MetadataWorkspace, MergeOption.AppendOnly, false, true });

                var enumerator = (IEnumerator<T>)shaper.GetType().GetMethod("GetEnumerator", ReflectionBindingFlags).Invoke(shaper, null);
                var results = new List<T>();

                try
                {
                    while (enumerator.MoveNext())
                    {
                        results.Add(enumerator.Current);
                    }
                }
                catch(Exception ex)
                {
                    throw new ApplicationException(
                        string.Format(
                            "An error occurred durring batch execution of the split queries. See inner exception for details. The query which failed was: \r\n \r\n \"{0}\" \r\n \r\n Parameter values: {1}", 
                            query.ToTraceString(),
                            string.Join(", ", query.Parameters.Select(p => string.Format("{0} = {1}", p.Name, p.Value == null ? "null" : p.Value.ToString())))),
                        ex);
                }
                return results;
            }
        }
    }
}
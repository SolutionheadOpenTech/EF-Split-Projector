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
            var objectQueries = queries.Select(q => q.GetObjectQuery()).ToList();
            var contexts = objectQueries.Select(q => q.Context).Distinct().ToList();
            if(contexts.Count != 1)
            {
                throw new Exception(string.Format("Expected queries with single distinct ObjectContext, but received {0} distinct ObjecContexts.", contexts.Count));
            }
            var context = contexts.Single();

            var contextType = context.GetType();
            var ensureConnectionInfo = contextType.GetMethod("EnsureConnection", ReflectionBindingFlags);
            ensureConnectionInfo.Invoke(context, new object[] { false });

            var results = new List<List<T>>();
            try
            {
                using(var command = CreateBatchCommand(objectQueries, context))
                using(var reader = command.ExecuteReader())
                {
                    foreach(var query in objectQueries)
                    {
                        results.Add(GetResults<T>(query, context, reader));
                        reader.NextResult();
                    }
                }
            }
            finally
            {
                contextType.GetMethod("ReleaseConnection", ReflectionBindingFlags).Invoke(context, null);
            }
            return results;
        }

        private static DbCommand CreateBatchCommand(IEnumerable<ObjectQuery> queries, ObjectContext objectContext)
        {
            var dbConnection = objectContext.Connection;
            var entityConnection = dbConnection as EntityConnection;

            var batchCommand = entityConnection == null ? dbConnection.CreateCommand() : entityConnection.StoreConnection.CreateCommand();
            var batchSql = new StringBuilder();
            var count = 0;

            foreach(var query in queries)
            {                
                var commandText = query.ToTraceString();
                foreach(var parameter in query.Parameters)
                {
                    var updatedName = string.Format("f{0}_{1}", count, parameter.Name);
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
                batchSql.Append(++count);
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

        private static List<T> GetResults<T>(ObjectQuery query, ObjectContext context, DbDataReader reader)
        {
            var queryState = query.GetType().GetProperty("QueryState", ReflectionBindingFlags).GetValue(query);
            var executionPlan = queryState.GetType().GetMethod("GetExecutionPlan", ReflectionBindingFlags).Invoke(queryState, new object[] { null });
            var shaperFactory = executionPlan.GetType().GetField("ResultShaperFactory", ReflectionBindingFlags).GetValue(executionPlan);
            var shaper = shaperFactory.GetType().GetMethod("Create", ReflectionBindingFlags).Invoke(shaperFactory, new object[] { reader, context, context.MetadataWorkspace, MergeOption.AppendOnly, false, true });

            var enumerator = (IEnumerator<T>)shaper.GetType().GetMethod("GetEnumerator", ReflectionBindingFlags).Invoke(shaper, null);
            var results = new List<T>();
            while(enumerator.MoveNext())
            {
                results.Add(enumerator.Current);
            }
            return results;
        }
    }
}
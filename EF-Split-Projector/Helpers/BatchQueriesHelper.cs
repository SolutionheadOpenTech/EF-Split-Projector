using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EF_Split_Projector.Helpers
{
    internal static class BatchQueriesHelper
    {
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
            var ensureConnectionInfo = contextType.GetMethod("EnsureConnection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
                contextType.GetMethod("ReleaseConnection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(context, null);
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
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var queryState = query.GetType().GetProperty("QueryState", flags).GetValue(query);
            var executionPlan = queryState.GetType().GetMethod("GetExecutionPlan", flags).Invoke(queryState, new object[] { null });
            var shaperFactory = executionPlan.GetType().GetField("ResultShaperFactory", flags).GetValue(executionPlan);
            var shaper = shaperFactory.GetType().GetMethod("Create", flags).Invoke(shaperFactory, new object[] { reader, context, context.MetadataWorkspace, MergeOption.AppendOnly, false, true });

            var enumerator = (IEnumerator<T>)shaper.GetType().GetMethod("GetEnumerator", flags).Invoke(shaper, null);
            var results = new List<T>();
            while(enumerator.MoveNext())
            {
                results.Add(enumerator.Current);
            }
            return results;
        }
    }
}
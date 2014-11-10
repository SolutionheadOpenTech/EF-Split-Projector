using System;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Reflection;

namespace EF_Split_Projector.Helpers.Extensions
{
    internal static class EntityFrameworkExtensions
    {
        public static ObjectQuery GetObjectQuery(this IQueryable query)
        {
            if(query == null) { return null; }

            var objectQuery = query as ObjectQuery;
            if(objectQuery == null)
            {
                var internalQueryInfo = query.GetType().GetProperty("InternalQuery", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if(internalQueryInfo != null)
                {
                    var internalQuery = internalQueryInfo.GetValue(query);
                    if(internalQuery != null)
                    {
                        var objectQueryInfo = internalQuery.GetType().GetProperty("ObjectQuery", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        objectQuery = (ObjectQuery) objectQueryInfo.GetValue(internalQuery);
                    }
                }
            }

            return objectQuery;
        }

        public static ObjectQuery<T> GetObjectQuery<T>(this IQueryable<T> query)
        {
            return (ObjectQuery<T>)GetObjectQuery((IQueryable)query);
        }

        public static ObjectContext GetObjectContext(this IQueryable query)
        {
            if(query == null) { throw new ArgumentNullException("query"); }

            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var internalContextInfo = query.Provider.GetType().GetProperty("InternalContext", bindingFlags);
            if(internalContextInfo == null)
            {
                var contextInfo = query.GetType().GetProperty("Context", bindingFlags);
                if(contextInfo == null)
                {
                    throw new Exception("Could not find property 'InternalContext' in source.Provider.");
                }
                return (ObjectContext) contextInfo.GetValue(query);
            }

            var internalContext = internalContextInfo.GetValue(query.Provider, null);
            var objectContextInfo = internalContext.GetType().GetProperty("ObjectContext", bindingFlags);
            if(objectContextInfo == null)
            {
                throw new Exception("Could not find property 'ObjectContext'.");
            }

            return (ObjectContext)objectContextInfo.GetValue(internalContext, null);
        }
    }
}
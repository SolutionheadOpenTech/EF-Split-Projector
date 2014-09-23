using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Reflection;

namespace EF_Split_Projector.Helpers
{
    internal static class EFHelper
    {
        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> EntityKeys = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        public static ObjectContext GetObjectContext(this IQueryable query)
        {
            if(query == null) { throw new ArgumentNullException("query"); }

            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var internalContextInfo = query.Provider.GetType().GetProperty("InternalContext", bindingFlags);
            if(internalContextInfo == null)
            {
                throw new Exception("Could not find property 'InternalContext' in source.Provider.");
            }

            var internalContext = internalContextInfo.GetValue(query.Provider, null);
            var objectContextInfo = internalContext.GetType().GetProperty("ObjectContext", bindingFlags);
            if(objectContextInfo == null)
            {
                throw new Exception("Could not find property 'ObjectContext'.");
            }

            return (ObjectContext)objectContextInfo.GetValue(internalContext, null);
        }

        public static Dictionary<string, PropertyInfo> GetKeyProperties(ObjectContext objectContext, Type entityType)
        {
            Dictionary<string, PropertyInfo> keys;
            if(!EntityKeys.TryGetValue(entityType, out keys))
            {
                var entityInfo = objectContext.MetadataWorkspace.GetItems<EntityType>(DataSpace.CSpace).SingleOrDefault(s => s.Name == entityType.Name);
                if(entityInfo != null)
                {
                    keys = entityInfo.KeyProperties.Select(k => entityType.GetProperty(k.Name)).ToDictionary(p => p.Name, p => p);
                }

                EntityKeys.Add(entityType, keys);
            }

            return keys;
        }
    }
}
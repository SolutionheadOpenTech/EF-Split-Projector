using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Reflection;

namespace EF_Split_Projector.Helpers
{
    internal class ObjectContextKeys
    {
        public Dictionary<string, PropertyInfo> this[Type entityType]
        {
            get
            {
                if(entityType == null)
                {
                    return null;
                }

                Dictionary<string, PropertyInfo> keys;
                if(!_entityKeys.TryGetValue(entityType, out keys))
                {
                    var entityInfo = _context.MetadataWorkspace.GetItems<EntityType>(DataSpace.CSpace).SingleOrDefault(s => s.Name == entityType.Name);
                    if(entityInfo != null)
                    {
                        keys = entityInfo.KeyProperties.Select(k => entityType.GetProperty(k.Name)).ToDictionary(p => p.Name, p => p);
                    }

                    _entityKeys.Add(entityType, keys);
                }

                return keys; 
            }
        }

        public ObjectContextKeys(ObjectContext context)
        {
            if(context == null) { throw new ArgumentNullException("context"); }
            _context = context;
        }

        private readonly ObjectContext _context;
        private readonly Dictionary<Type, Dictionary<string, PropertyInfo>> _entityKeys = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace EF_Split_Projector.Helpers
{
    internal static class UniqueTypeBuilder
    {
        public static Type GetUniqueType(IDictionary<string, Type> members, ISet<Type> usedTypes)
        {
            List<Type> newTypes;
            var membersHash = GetMembersHash(members);
            if(!DynamicTypes.TryGetValue(membersHash, out newTypes))
            {
                DynamicTypes.Add(membersHash, newTypes = new List<Type>());
            }

            var newType = newTypes.FirstOrDefault(t => usedTypes == null || !usedTypes.Contains(t));
            if(newType == null)
            {
                var newDefinition = ModuleBuilder.DefineType(string.Format("{0}#{1}", membersHash, newTypes.Count),
                                                             TypeAttributes.Public |
                                                             TypeAttributes.Class |
                                                             TypeAttributes.AutoClass |
                                                             TypeAttributes.AnsiClass |
                                                             TypeAttributes.BeforeFieldInit |
                                                             TypeAttributes.AutoLayout);
                foreach(var member in members)
                {
                    newDefinition.DefineField(member.Key, member.Value, FieldAttributes.Public);
                }
                newTypes.Add(newType = newDefinition.CreateType());
            }

            if(usedTypes != null)
            {
                usedTypes.Add(newType);
            }
            return newType;
        }

        private static ModuleBuilder ModuleBuilder
        {
            get
            {
                if(_moduleBuilder == null)
                {
                    var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("EFSplitProjectorTypeAssembly"), AssemblyBuilderAccess.Run);
                    _moduleBuilder = assemblyBuilder.DefineDynamicModule("EFSplitProjectorTypeModule");
                }
                return _moduleBuilder;
            }
        }
        private static ModuleBuilder _moduleBuilder;
        private static readonly Dictionary<int, List<Type>> DynamicTypes = new Dictionary<int, List<Type>>();

        private static int GetMembersHash(IEnumerable<KeyValuePair<string, Type>> members)
        {
            return members
                .Select(m => string.Format("{0}:{1}", m.Key, m.Value.AssemblyQualifiedName))
                .OrderBy(n => n)
                .Aggregate(0, (c, n) => (c * 397) ^ n.GetHashCode());
        }
    }
}
using System;
using System.Collections.Concurrent;
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
            var membersHash = GetMembersHash(members);
            var newTypes = DynamicTypes.GetOrAdd(membersHash, new List<Type>());

            var newType = newTypes.FirstOrDefault(t => usedTypes == null || !usedTypes.Contains(t));
            if(newType == null)
            {
                var newTypeName = string.Format("{0}#{1}", membersHash, newTypes.Count);

                newType = ModuleBuilder.GetTypes().FirstOrDefault(t => t.Name == newTypeName);
                if(newType == null)
                {
                    var newDefinition = ModuleBuilder.DefineType(newTypeName,
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

                    newType = newDefinition.CreateType();
                }

                newTypes.Add(newType);
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
                    var assemblyName = new AssemblyName("EFSplitProjectorTypeAssembly");
                    var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                    _moduleBuilder = assemblyBuilder.DefineDynamicModule("EFSplitProjectorTypeModule");
                }
                return _moduleBuilder;
            }
        }
        private static ModuleBuilder _moduleBuilder;
        private static readonly ConcurrentDictionary<int, List<Type>> DynamicTypes = new ConcurrentDictionary<int, List<Type>>();

        private static int GetMembersHash(IEnumerable<KeyValuePair<string, Type>> members)
        {
            return string.Join(";", members
                .Select(m => string.Format("{0}:{1}", m.Key, m.Value.AssemblyQualifiedName))
                .OrderBy(n => n)).GetHashCode();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace EF_Split_Projector.Helpers
{
    internal static class DerivedTypeBuilder
    {
        private static ModuleBuilder ModuleBuilder
        {
            get
            {
                if(_moduleBuilder == null)
                {
                    var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("EFSplitProjectorDynamicTypeAssembly"), AssemblyBuilderAccess.Run);
                    _moduleBuilder = assemblyBuilder.DefineDynamicModule("EFSplitProjectorDynamicTypeModule");
                }
                return _moduleBuilder;
            }
        }
        private static ModuleBuilder _moduleBuilder;

        private static readonly Dictionary<Type, Type> DerivedTypes = new Dictionary<Type, Type>();

        internal static Type BuildDerivedType(Type baseType)
        {
            Type derivedType;
            if(!DerivedTypes.TryGetValue(baseType, out derivedType))
            {
                var className = string.Format("EFSplitProjectorClass:{0}.{1}", baseType.Namespace, baseType.Name);
                DerivedTypes.Add(baseType, derivedType = ModuleBuilder.DefineType(className,
                                     TypeAttributes.Public |
                                     TypeAttributes.Class |
                                     TypeAttributes.AutoClass |
                                     TypeAttributes.AnsiClass |
                                     TypeAttributes.BeforeFieldInit |
                                     TypeAttributes.AutoLayout,
                                     baseType).CreateType());
            }
            return derivedType;
        }
    }
}

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace EF_Split_Projector.Helpers
{
    internal static class DerivedTypeBuilder
    {
        private static ModuleBuilder _moduleBuilder;
        private static int _typeCount;
        internal static Type BuildDerivedType(Type baseType)
        {
            if(_moduleBuilder == null)
            {
                var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("EFSplitProjectorDynamicTypeAssembly"), AssemblyBuilderAccess.Run);
                _moduleBuilder = assemblyBuilder.DefineDynamicModule("EFSplitProjectorDynamicTypeModule");
            }

            return _moduleBuilder.DefineType(string.Format("EFSplitProjectorDynamicType{0}", _typeCount++),
                                     TypeAttributes.Public |
                                     TypeAttributes.Class |
                                     TypeAttributes.AutoClass |
                                     TypeAttributes.AnsiClass |
                                     TypeAttributes.BeforeFieldInit |
                                     TypeAttributes.AutoLayout,
                                     baseType).CreateType();
        }
    }
}

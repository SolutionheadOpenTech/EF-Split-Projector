using System;
using System.Linq;
using System.Reflection;

namespace EF_Split_Projector.Helpers.Extensions
{
    public static class MemberInfoExtensions
    {
        public static Type GetMemberType(this MemberInfo memberInfo)
        {
            switch(memberInfo.MemberType)
            {
                case MemberTypes.Field: return ((FieldInfo)memberInfo).FieldType;
                case MemberTypes.Property: return ((PropertyInfo)memberInfo).PropertyType;
            }

            return null;
        }

        public static bool IsOrImplements(this MemberInfo memberInfo, MemberInfo other, Type derivedParent = null)
        {
            if(memberInfo == other)
            {
                return true;
            }

            var thisProperty = memberInfo as PropertyInfo;
            if(thisProperty != null)
            {
                return thisProperty.IsOrImplements(other as PropertyInfo, derivedParent);
            }

            var thisMethod = memberInfo as MethodInfo;
            if(thisMethod != null)
            {
                return thisMethod.IsOrImplements(other as MethodInfo);
            }

            return false;
        }

        public static bool IsOrImplements(this PropertyInfo thisProperty, PropertyInfo otherProperty, Type derivedParent = null)
        {
            if(thisProperty == otherProperty)
            {
                return true;
            }

            if(thisProperty != null && otherProperty != null && thisProperty.DeclaringType != null && otherProperty.DeclaringType != null)
            {
                if(otherProperty.DeclaringType.IsInterface)
                {
                    return PropertyImplements(thisProperty.DeclaringType, thisProperty, otherProperty) || PropertyImplements(derivedParent, thisProperty, otherProperty);
                }
            }

            return false;
        }

        public static bool IsOrImplements(this MethodInfo thisMethod, MethodInfo otherMethod)
        {
            if(thisMethod == otherMethod)
            {
                return true;
            }

            if(thisMethod != null && otherMethod != null && thisMethod.DeclaringType != null && otherMethod.DeclaringType != null)
            {
                if(thisMethod.IsGenericMethod && otherMethod.IsGenericMethodDefinition)
                {
                    return thisMethod.GetGenericMethodDefinition() == otherMethod;
                }
            }

            return false;
        }

        private static bool PropertyImplements(Type parentType, PropertyInfo prop, PropertyInfo interfaceProperty)
        {
            if(parentType == null)
            {
                return false;
            }

            var declaringInterface = interfaceProperty.DeclaringType;
            if(declaringInterface == null)
            {
                return false;
            }

            var interfaces = parentType.GetInterfaces().ToList();
            var implemented = interfaces.FirstOrDefault(i => i == declaringInterface);
            if(implemented == null && declaringInterface.IsGenericTypeDefinition)
            {
                implemented = interfaces.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == declaringInterface);
                if(implemented != null)
                {
                    interfaceProperty = implemented.GetProperty(interfaceProperty.Name, BindingFlags.Public | BindingFlags.Instance);
                }
            }

            if(implemented != null)
            {
                var thisGet = prop.GetGetMethod(false) ?? prop.GetGetMethod(true);
                var thisSet = prop.GetSetMethod(false) ?? prop.GetSetMethod(true);
                var interfaceGet = interfaceProperty.GetGetMethod();
                var interfaceSet = interfaceProperty.GetSetMethod();

                var mapping = parentType.GetInterfaceMap(implemented);
                for(var i = 0; i < mapping.TargetMethods.Count(); ++i)
                {
                    if(interfaceGet != null)
                    {
                        if(mapping.InterfaceMethods[i].MethodHandle == interfaceGet.MethodHandle && mapping.TargetMethods[i].MethodHandle == thisGet.MethodHandle)
                        {
                            return true;
                        }
                    }

                    if(interfaceSet != null)
                    {
                        if(mapping.InterfaceMethods[i].MethodHandle == interfaceSet.MethodHandle && mapping.TargetMethods[i].MethodHandle == thisSet.MethodHandle)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
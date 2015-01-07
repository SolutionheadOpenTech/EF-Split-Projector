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

        public static bool IsOrImplements(this MemberInfo memberInfo, MemberInfo other)
        {
            if(memberInfo == other)
            {
                return true;
            }

            var thisProperty = memberInfo as PropertyInfo;
            if(thisProperty != null)
            {
                return thisProperty.IsOrImplements(other as PropertyInfo);
            }

            var thisMethod = memberInfo as MethodInfo;
            if(thisMethod != null)
            {
                return thisMethod.IsOrImplements(other as MethodInfo);
            }

            return false;
        }

        public static bool IsOrImplements(this PropertyInfo thisProperty, PropertyInfo otherProperty)
        {
            if(thisProperty == otherProperty)
            {
                return true;
            }

            if(thisProperty != null && otherProperty != null && thisProperty.DeclaringType != null && otherProperty.DeclaringType != null)
            {
                if(otherProperty.DeclaringType.IsInterface)
                {
                    var interfaces = thisProperty.DeclaringType.GetInterfaces().ToList();
                    var implemented = interfaces.FirstOrDefault(i => i == otherProperty.DeclaringType);

                    if(otherProperty.DeclaringType.IsGenericTypeDefinition)
                    {
                        implemented = interfaces.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == otherProperty.DeclaringType);
                    }

                    if(implemented != null)
                    {
                        var thisGet = thisProperty.GetGetMethod(false) ?? thisProperty.GetGetMethod(true);
                        var thisSet = thisProperty.GetSetMethod(false) ?? thisProperty.GetSetMethod(true);
                        return (thisProperty.DeclaringType.GetInterfaceMap(implemented).TargetMethods ?? new MethodInfo[0])
                            .Any(m => (thisGet != null && m == thisGet) || (thisSet != null && m == thisSet));
                    }
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
    }

}
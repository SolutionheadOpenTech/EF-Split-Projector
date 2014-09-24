using System;
using System.Linq;

namespace EF_Split_Projector.Helpers.Extensions
{
    public static class TypeExtensions
    {
        public static Type GetGenericInterfaceImplementation(this Type t, Type interfaceDefinition)
        {
            if(!interfaceDefinition.IsInterface || !interfaceDefinition.IsGenericTypeDefinition)
            {
                throw new ArgumentException("TDefinition must be a generic interface definition.");
            }

            var interfaces = t.GetInterfaces().Where(i => i.IsGenericType).ToList();
            if(t.IsInterface && t.IsGenericType)
            {
                interfaces.Insert(0, t);
            }

            return interfaces.FirstOrDefault(i => i.GetGenericTypeDefinition() == interfaceDefinition);
        }

        /// <summary>
        /// Returns whether or not this type is or implements otherType.
        /// </summary>
        public static bool IsOrImplementsType(this Type type, Type otherType)
        {
            if(type == otherType)
            {
                return true;
            }

            if(otherType.IsAssignableFrom(type))
            {
                return true;
            }

            if(type.IsInterface)
            {
                if(otherType.GetInterfaces().Any(type.IsAssignableFrom))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsOrImplementsType<T>(this Type type)
        {
            return type.IsOrImplementsType(typeof(T));
        }

        public static bool IsComplexType(this Type type)
        {
            if(type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(DateTime))
            {
                return false;
            }

            if(type.IsGenericType)
            {
                var genericDefinition = type.GetGenericTypeDefinition();
                if(genericDefinition == typeof(Nullable<>))
                {
                    return IsComplexType(type.GetGenericArguments().Single());
                }
            }

            return type.IsClass || type.IsValueType || type.IsInterface;
        }
    }
}
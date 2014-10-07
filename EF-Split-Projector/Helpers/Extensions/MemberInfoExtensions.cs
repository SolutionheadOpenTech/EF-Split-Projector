using System;
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
    }
}
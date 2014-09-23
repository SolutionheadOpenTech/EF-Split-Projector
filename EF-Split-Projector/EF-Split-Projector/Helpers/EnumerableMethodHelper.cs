using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EF_Split_Projector.Helpers.Extensions;

namespace EF_Split_Projector.Helpers
{
    internal static class EnumerableMethodHelper
    {
        public static EnumerableType GetEnumerableType(MethodInfo orderByMethod, out Type enumeratedType)
        {
            var enumerableType = EnumerableType.None;

            if(orderByMethod != null && orderByMethod.IsGenericMethod)
            {
                var methodDefinition = orderByMethod.GetGenericMethodDefinition();
                if(QueryableOrderByMethods.Contains(methodDefinition) || QueryableThenByMethods.Contains(methodDefinition))
                {
                    enumerableType = EnumerableType.Queryable;
                }
                else if(EnumerableOrderByMethods.Contains(methodDefinition) || EnumerableThenByMethods.Contains(methodDefinition))
                {
                    enumerableType = EnumerableType.Enumerable;
                }
            }

            enumeratedType = enumerableType == EnumerableType.None ? null : orderByMethod.GetParameters().First().ParameterType.GetGenericArguments().Single();
            return enumerableType;
        }

        public static EnumerableType GetEnumerableType(MemberInfo memberInfo, out Type enumeratedType)
        {
            return GetEnumerableType(memberInfo.GetMemberType(), out enumeratedType);
        }

        public static EnumerableType GetEnumerableType(Type type, out Type enumeratedType)
        {
            var enumerableType = EnumerableType.None;
            Type typeOfEnumerable = null;

            if(type != null)
            {
                var genericInterfaces = type.GetInterfaces().Where(i => i.IsGenericType).ToList();
                if(type.IsInterface && type.IsGenericType)
                {
                    genericInterfaces.Add(type);
                }

                if((typeOfEnumerable = type.GetGenericInterfaceImplementation(QueryableDefinition)) != null)
                {
                    enumerableType = EnumerableType.Queryable;
                }
                else if((typeOfEnumerable = type.GetGenericInterfaceImplementation(EnumerableDefinition)) != null)
                {
                    enumerableType = EnumerableType.Enumerable;
                }
            }

            enumeratedType = typeOfEnumerable == null ? null : typeOfEnumerable.GetGenericArguments().Single();
            return enumerableType;
        }

        public static Expression AppendOrderByExpressions(Expression expression, EnumerableType enumerableType, Type enumerableSourceType, IEnumerable<MemberInfo> orderKeys, bool startWithThenBy)
        {
            var orderBy = !startWithThenBy;
            foreach(var key in orderKeys)
            {
                expression = AppendOrderByMethod(expression, enumerableType, enumerableSourceType, key, !orderBy);
                orderBy = false;
            }
            return expression;
        }

        private static Expression AppendOrderByMethod(Expression expression, EnumerableType enumerableType, Type enumerableSourceType, MemberInfo keyMember, bool thenBy)
        {
            var memberType = keyMember.GetMemberType();
            if(memberType == null)
            {
                throw new Exception("Could not determine keyMember type.");
            }

            var method = GetOrderByThenByMethod(enumerableType, thenBy, enumerableSourceType, memberType);
            var parameterExpression = Expression.Parameter(enumerableSourceType, enumerableSourceType.Name);
            var memberAccess = Expression.MakeMemberAccess(parameterExpression, keyMember);
            var keySelector = Expression.Lambda(memberAccess, parameterExpression);

            switch(enumerableType)
            {
                case EnumerableType.Enumerable: return Expression.Call(null, method, new[] { expression, keySelector });
                case EnumerableType.Queryable: return Expression.Call(null, method, new[] { expression, Expression.Quote(keySelector) });
            }
            throw new ArgumentOutOfRangeException("enumerableType");
        }

        private static MethodInfo GetOrderByThenByMethod(EnumerableType enumerableType, bool thenBy, Type sourceType, Type memberType)
        {
            switch(enumerableType)
            {
                case EnumerableType.Enumerable:
                    return (thenBy ? EnumerableThenByMethods.Where(m => m.Name == "ThenBy") : EnumerableOrderByMethods.Where(m => m.Name == "OrderBy"))
                        .Single(m => m.GetParameters().Length == 2)
                        .MakeGenericMethod(sourceType, memberType);

                case EnumerableType.Queryable:
                    return (thenBy ? QueryableThenByMethods.Where(m => m.Name == "ThenBy") : QueryableOrderByMethods.Where(m => m.Name == "OrderBy"))
                        .Single(m => m.GetParameters().Length == 2)
                        .MakeGenericMethod(sourceType, memberType);

                default: throw new Exception("Invalid enumerableType.");
            }
        }

        public enum EnumerableType
        {
            None,
            Enumerable,
            Queryable
        }

        private static readonly Type EnumerableDefinition = typeof(IEnumerable<>);
        private static readonly Type QueryableDefinition = typeof(IQueryable<>);

        private static readonly List<MethodInfo> QueryableMethods = typeof(Queryable).GetMethods(BindingFlags.Static | BindingFlags.Public).ToList();
        public static readonly HashSet<MethodInfo> QueryableOrderByMethods = MakeHashSet(QueryableMethods.Where(m => m.Name.StartsWith("OrderBy")));
        public static readonly HashSet<MethodInfo> QueryableThenByMethods = MakeHashSet(QueryableMethods.Where(m => m.Name.StartsWith("ThenBy")));

        private static readonly List<MethodInfo> EnumerableMethods = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public).ToList();
        public static readonly HashSet<MethodInfo> EnumerableOrderByMethods = MakeHashSet(EnumerableMethods.Where(m => m.Name.StartsWith("OrderBy")));
        public static readonly HashSet<MethodInfo> EnumerableThenByMethods = MakeHashSet(EnumerableMethods.Where(m => m.Name.StartsWith("ThenBy")));

        private static HashSet<T> MakeHashSet<T>(IEnumerable<T> source)
        {
            var hashSet = new HashSet<T>();
            foreach(var memeber in source.Distinct())
            {
                hashSet.Add(memeber);
            }
            return hashSet;
        }
    }
}
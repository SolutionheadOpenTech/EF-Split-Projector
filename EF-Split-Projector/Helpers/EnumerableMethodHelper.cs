using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EF_Split_Projector.Helpers.Extensions;
using EF_Split_Projector.Helpers.Visitors;

namespace EF_Split_Projector.Helpers
{
    internal static class EnumerableMethodHelper
    {
        public static Func<IQueryable, object> ToCreateQueryDelegate(MethodCallExpression methodCall)
        {
            return q => q.Provider.CreateQuery(Expression.Call(null, methodCall.Method, methodCall.Arguments.Select((t, i) => i == 0 ? q.Expression : RemoveMergeAsVisitor.RemoveMergeAs(t)).ToArray()));
        }

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
            var addSkip = false;
            foreach(var key in orderKeys)
            {
                expression = AppendOrderByMethod(expression, enumerableType, enumerableSourceType, key, !orderBy);
                orderBy = false;
                addSkip = true;
            }
            if(addSkip)
            {
                expression = AppendSkipMethod(expression, enumerableType, enumerableSourceType);
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

        private static Expression AppendSkipMethod(Expression expression, EnumerableType enumerableType, Type enumerableSourceType)
        {
            var method = GetSkipMethod(enumerableType, enumerableSourceType);
            var constantZero = Expression.Constant(0, typeof(int));
            return Expression.Call(null, method, new[] { expression, constantZero });
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

        private static MethodInfo GetSkipMethod(EnumerableType enumerableType, Type sourceType)
        {
            switch(enumerableType)
            {
                case EnumerableType.Enumerable:
                    return EnumerableSkipMethods.Where(m => m.Name == "Skip")
                        .Single(m => m.GetParameters().Length == 2)
                        .MakeGenericMethod(sourceType);

                case EnumerableType.Queryable:
                    return QueryableSkipMethods.Where(m => m.Name == "Skip")
                        .Single(m => m.GetParameters().Length == 2)
                        .MakeGenericMethod(sourceType);

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
        public static readonly HashSet<MethodInfo> QueryableOrderByMethods = QueryableMethods.Where(m => m.Name.StartsWith("OrderBy")).ToHashSet();
        public static readonly HashSet<MethodInfo> QueryableThenByMethods = QueryableMethods.Where(m => m.Name.StartsWith("ThenBy")).ToHashSet();
        public static readonly HashSet<MethodInfo> QueryableSkipMethods = QueryableMethods.Where(m => m.Name.StartsWith("Skip")).ToHashSet();

        private static readonly List<MethodInfo> EnumerableMethods = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public).ToList();
        public static readonly HashSet<MethodInfo> EnumerableOrderByMethods = EnumerableMethods.Where(m => m.Name.StartsWith("OrderBy")).ToHashSet();
        public static readonly HashSet<MethodInfo> EnumerableThenByMethods = EnumerableMethods.Where(m => m.Name.StartsWith("ThenBy")).ToHashSet();
        public static readonly HashSet<MethodInfo> EnumerableSkipMethods = EnumerableMethods.Where(m => m.Name.StartsWith("Skip")).ToHashSet();
    }
}
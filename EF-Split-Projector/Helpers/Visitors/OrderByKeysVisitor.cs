using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EF_Split_Projector.Helpers.Extensions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class OrderByKeysVisitor : ExpressionVisitor
    {
        /// <summary>
        /// Takes in an IQueryable of T and returns an IQueryable of T that has OrderBy/ThenBy clauses injected to ensure that all enumerable references that are of an entity type
        /// defined in the source query's object context are ordered by their primary keys.
        /// </summary>
        public static IQueryable<T> InjectOrderByEntityKeys<T>(IQueryable<T> source)
        {
            var context = source.GetObjectContext();
            var newExpression = new OrderByKeysVisitor(context).Visit(source.Expression);
            return source.Provider.CreateQuery<T>(newExpression);
        }

        private readonly ObjectContextKeys _keys;

        private OrderByKeysVisitor(ObjectContext objectContext)
        {
            _keys = new ObjectContextKeys(objectContext);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            /* First check to see if where are dealing with the root queryable node, which will also need to be ordered by it's primary keys.
             * This is currently depending on what looks to be an EF-specific method call off the root queryable called "MergeAs", if this method
             * *isn't* present at the root as expected then the root elements will *not* be ordered, which would break a dependent expectation. -RI 2014/09/17
             */
            if(node.Object != null)
            {
                Type enumeratedType;
                var enumerableType = EnumerableMethodHelper.GetEnumerableType(node.Object.Type, out enumeratedType);
                if(enumerableType != EnumerableMethodHelper.EnumerableType.None)
                {
                    var keys = _keys[enumeratedType];
                    if(keys != null && node.Method.Name == "MergeAs")
                    {
                        return EnumerableMethodHelper.AppendOrderByExpressions(node, enumerableType, enumeratedType, keys.Values, false);
                    }
                }
            }

            return GetAppendedOrderByKeysExpression(node, true, EnumerableMethodHelper.GetEnumerableType, node.Method) ?? base.VisitMethodCall(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return GetAppendedOrderByKeysExpression(node, false, EnumerableMethodHelper.GetEnumerableType, node.Type) ?? base.VisitParameter(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            return GetAppendedOrderByKeysExpression(node, false, EnumerableMethodHelper.GetEnumerableType, node.Member) ?? base.VisitMember(node);
        }

        private Expression GetAppendedOrderByKeysExpression<T>(Expression expression, bool thenByFirst, GetEnumerableTypeMethod<T> getEnumerableTypeMethod, T parameter)
        {
            Type enumeratedType;
            var enumerableType = getEnumerableTypeMethod(parameter, out enumeratedType);
            if(enumerableType != EnumerableMethodHelper.EnumerableType.None && enumeratedType != null)
            {
                IDictionary<string, PropertyInfo> keys;
                try
                {
                    keys = _keys[enumeratedType];
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(
                        string.Format(
                            "EF Split Query was unable to determine the key properties for the type \"{0}\". See inner exception for details.", 
                            enumeratedType.FullName), 
                        ex);
                }

                if(keys != null)
                {
                    return EnumerableMethodHelper.AppendOrderByExpressions(expression, enumerableType, enumeratedType, keys.Values, thenByFirst);
                }
            }

            return null;
        }

        private delegate EnumerableMethodHelper.EnumerableType GetEnumerableTypeMethod<in T>(T parameter, out Type enumeratedType);
    }
}
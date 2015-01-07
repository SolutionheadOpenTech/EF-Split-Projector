using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EF_Split_Projector.Helpers.Extensions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class TranslateExpressionVisitor<TSource, TDest> : ExpressionVisitor
    {
        internal static MethodCallExpression TranslateMethodCall(MethodCallExpression methodCall, Expression firstArgumentReplacement, Expression<Func<TSource, TDest>> projector)
        {
            Logging.Start("TranslateMethodCall");
            var arguments = methodCall.Arguments.ToList().Select((a, i) => i == 0 ? firstArgumentReplacement : new TranslateExpressionVisitor<TSource, TDest>(methodCall, projector).Visit(a)).ToList();
            var newMethodInfo = methodCall.Method.UpdateGenericArguments(arguments.Select(a => a.Type));
            return Expression.Call(newMethodInfo, arguments);
        }

        /// <summary>
        /// Returns the equivalent of sourceExpression where references in sourceExpression rooted in TProjectorDest
        /// are translated to references to TProjectorSource according to their assignment defined in the projectors supplied;.
        /// </summary>
        private TranslateExpressionVisitor(Expression source, LambdaExpression projector)
        {
            _projector = projector;
            _memberInfos = GatherDistinctMemberInfosVisitor.GetMemberInfos(source);
        }

        private readonly LambdaExpression _projector;
        private readonly HashSet<MemberInfo> _memberInfos;

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var newParameters = node.Parameters.Select(p => p.Type.IsOrImplementsType<TDest>() ? _projector.Parameters.Single() : p).ToList();
            var newBody = base.Visit(node.Body);

            return Expression.Lambda(newBody, newParameters);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            var expression = node.Type.IsOrImplementsType<TDest>() ? Visit(_projector.Body) : base.VisitParameter(node);
            return expression;
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            var visitedBindings = node.Bindings
                .Where(b => _memberInfos.Any(m => b.Member.IsOrImplements(m)))
                .Select(b =>
                {
                    var memberAssignment = (MemberAssignment)b;
                    if(memberAssignment == null)
                    {
                        throw new Exception(string.Format("Expected MemberBindingType 'Assignment', but received '{0}'.", b.BindingType));
                    }
                    return new
                    {
                        b.Member,
                        Assignment = Visit(memberAssignment.Expression)
                    };
                }).ToList();
            var newType = UniqueTypeBuilder.GetUniqueType(visitedBindings.ToDictionary(m => m.Member.Name, m => m.Assignment.Type), null);
            var newBindings = visitedBindings.Select(b =>
            {
                var member = newType.GetMember(b.Member.Name, BindingFlags.Public | BindingFlags.Instance);
                return Expression.Bind(member.First(), b.Assignment);
            }).ToList();

            return Expression.MemberInit(Expression.New(newType), newBindings);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if(node.Method.IsGenericMethod)
            {
                var visitedArguments = node.Arguments.Select(Visit).ToList();
                var newMethodInfo = node.Method.UpdateGenericArguments(visitedArguments.Select(a => a.Type));
                return Expression.Call(newMethodInfo, visitedArguments);
            }

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if(node.Expression != null)
            {
                var nodeExpression = Visit(node.Expression);
                var member = nodeExpression.Type.GetMember(node.Member.Name, BindingFlags.Public | BindingFlags.Instance).Single();
                return Expression.MakeMemberAccess(nodeExpression, member);
            }

            return base.VisitMember(node);
        }
    }
}
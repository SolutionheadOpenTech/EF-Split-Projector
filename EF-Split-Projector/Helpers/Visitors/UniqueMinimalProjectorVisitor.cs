using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EF_Split_Projector.Helpers.Extensions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class UniqueMinimalProjectorVisitor : ExpressionVisitor
    {
        internal static LambdaExpression CreateUniqueMinimalProjector(Expression source, LambdaExpression projector)
        {
            var memberInfos = GatherDistinctMemberInfosVisitor.GetMemberInfos(source);
            return (LambdaExpression) new UniqueMinimalProjectorVisitor(memberInfos).Visit(projector);
        }

        private readonly IEnumerable<MemberInfo> _memberInfos;
        private readonly HashSet<Type> _usedTypes = new HashSet<Type>();

        private UniqueMinimalProjectorVisitor(IEnumerable<MemberInfo> memberInfos)
        {
            _memberInfos = memberInfos;
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
            var newType = UniqueTypeBuilder.GetUniqueType(visitedBindings.ToDictionary(m => m.Member.Name, m => m.Assignment.Type), _usedTypes);
            var newBindings = visitedBindings.Select(b =>
                {
                    var member = newType.GetMember(b.Member.Name, BindingFlags.Public | BindingFlags.Instance);
                    return Expression.Bind(member.First(), b.Assignment);
                }).ToList();
            return Expression.MemberInit(Expression.New(newType), newBindings);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            return Expression.Lambda(base.Visit(node.Body), node.Parameters);
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
    }
}
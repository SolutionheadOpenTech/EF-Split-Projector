using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EF_Split_Projector.Helpers.Extensions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class TranslateExpressionVisitor : ExpressionVisitor
    {
        internal static MethodCallExpression TranslateMethodCall<TSource, TDest>(MethodCallExpression methodCall, Expression firstArgumentReplacement, Expression<Func<TSource, TDest>> projector)
        {
            var translatedArguments = methodCall.Arguments.Select((a, i) => i == 0 ? firstArgumentReplacement : TranslateFromProjector(a, projector)).ToArray();
            var genericMethodDefinition = methodCall.Method.GetGenericMethodDefinition();
            var typeArguments = methodCall.Method.GetGenericArguments().Select(a => a.ReplaceType(typeof(TDest), typeof(TSource))).ToArray();
            var methodInfo = genericMethodDefinition.MakeGenericMethod(typeArguments);

            return Expression.Call(null, methodInfo, translatedArguments);
        }

        /// <summary>
        /// Returns the equivalent of sourceExpression where references in sourceExpression rooted in TProjectorDest
        /// are translated to references to TProjectorSource according to their assignment defined in the projectors supplied;.
        /// </summary>
        internal static Expression TranslateFromProjector<TSource, TDest>(Expression source, Expression<Func<TSource, TDest>> projector)
        {
            var memberReferences = GetRootedMemberVisitor<TDest>.GetRootedMembers(source);
            if(memberReferences.Any())
            {
                var memberExpressionMappings = new Dictionary<MemberExpression, LambdaExpression>();
                foreach(var member in memberReferences)
                {
                    var assignment = GetMemberAssignmentVisitor.GetMemberAssignment(member, projector);
                    if(assignment == null)
                    {
                        throw new Exception(string.Format("No equivalent expression found in projectors for: {0}", member));
                    }
                    memberExpressionMappings.Add(member, Expression.Lambda(assignment, projector.Parameters));
                }

                var visitor = new TranslateExpressionVisitor();
                var translated = visitor.FromProjectors(source, memberExpressionMappings);
                var derived = UniqueMemberInitTypeVisitor.MakeMemberInitTypeUnique(translated, visitor._visitedMembers);
                return derived;
            }

            return source;
        }

        private HashSet<MemberInfo> _visitedMembers;
        private Dictionary<MemberExpression, LambdaExpression> _memberExpressionMappings;
        private List<ParameterExpression> _lambdaParameters;

        private Expression FromProjectors(Expression sourceExpression, Dictionary<MemberExpression, LambdaExpression> memberExpressionMappings)
        {
            if(memberExpressionMappings == null) { throw new ArgumentNullException("memberExpressionMappings"); }
            _memberExpressionMappings = memberExpressionMappings;
            _visitedMembers = new HashSet<MemberInfo>();

            return Visit(sourceExpression);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var visitedExpression = base.VisitLambda(node);

            var visitedLambda = visitedExpression as LambdaExpression;
            if(_lambdaParameters != null && visitedLambda != null && node.Body != visitedLambda.Body)
            {
                visitedLambda = Expression.Lambda(visitedLambda.Body, _lambdaParameters.Distinct().ToArray());
                _lambdaParameters = null;
                return visitedLambda;
            }
            return visitedExpression;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            LambdaExpression equivalent;
            if(_memberExpressionMappings.TryGetValue(node, out equivalent))
            {
                _lambdaParameters = equivalent.Parameters.ToList();
                return equivalent.Body;
            }

            if(!_visitedMembers.Contains(node.Member))
            {
                _visitedMembers.Add(node.Member);
            }

            return base.VisitMember(node);
        }

        private static readonly MethodInfo TranslateEnumerableMethodCallInfo;

        static TranslateExpressionVisitor()
        {
            TranslateEnumerableMethodCallInfo = typeof(TranslateExpressionVisitor).GetMethod("TranslateMethodCall", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if(TranslateEnumerableMethodCallInfo == null)
            {
                throw new Exception("Could not find TranslateExpressionVisitor.TranslateMethodCall method.");
            }
        }
    }
}
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
        public static MethodCallExpression TranslateMethodCall<TSource, TDest>(MethodCallExpression methodCall, Expression newSource, params Expression<Func<TSource, TDest>>[] projectors)
        {
            var translatedArguments = methodCall.Arguments.Select((a, i) => i == 0 ? newSource : TranslateFromProjectors(a, projectors)).ToArray();
            var genericMethodDefinition = methodCall.Method.GetGenericMethodDefinition();
            var typeArguments = methodCall.Method.GetGenericArguments().Select(a => a.ReplaceType(typeof(TDest), typeof(TSource))).ToArray();
            var methodInfo = genericMethodDefinition.MakeGenericMethod(typeArguments);

            return Expression.Call(null, methodInfo, translatedArguments);
        }

        /// <summary>
        /// Returns the equivalent of sourceExpression where references in sourceExpression rooted in TProjectorDest
        /// are translated to references to TProjectorSource according to their assignment defined in the projectors supplied;.
        /// </summary>
        public static Expression TranslateFromProjectors<TSource, TDest>(Expression sourceExpression, params Expression<Func<TSource, TDest>>[] projectors)
        {
            var memberReferences = GetRootedMemberVisitor<TDest>.GetRootedMembers(sourceExpression);
            if(memberReferences.Any())
            {
                var memberExpressionMappings = new Dictionary<MemberExpression, LambdaExpression>();
                Expression<Func<TSource, TDest>> first = null;
                foreach(var member in memberReferences)
                {
                    Expression assignment = null;
                    foreach(var projector in projectors)
                    {
                        assignment = GetMemberAssignmentVisitor.GetMemberAssignment(member, projector);
                        if(assignment != null)
                        {
                            var mergedProjector = projector;
                            if(first == null)
                            {
                                first = projector;
                            }
                            else
                            {
                                mergedProjector = ReplaceParametersVisitor.MergeLambdaParameters(projector, first);
                            }
                            
                            memberExpressionMappings.Add(member, Expression.Lambda(assignment, mergedProjector.Parameters));
                            break;
                        }
                    }

                    if(assignment == null)
                    {
                        throw new Exception(string.Format("No equivalent expression found in projectors for: {0}", member));
                    }
                }

                var translated = new TranslateExpressionVisitor().FromProjectors(sourceExpression, memberExpressionMappings);
                return UniqueMemberInitTypeVisitor.MakeMemberInitTypeUnique(translated);
            }

            return sourceExpression;
        }

        private Dictionary<MemberExpression, LambdaExpression> _memberExpressionMappings;
        private List<ParameterExpression> _lambdaParameters;

        private Expression FromProjectors(Expression sourceExpression, Dictionary<MemberExpression, LambdaExpression> memberExpressionMappings)
        {
            if(memberExpressionMappings == null) { throw new ArgumentNullException("memberExpressionMappings"); }
            _memberExpressionMappings = memberExpressionMappings;

            return Visit(sourceExpression);
        }

        //protected override Expression VisitMethodCall(MethodCallExpression node)
        //{
        //    var visitedNode = base.VisitMethodCall(node);

        //    var visitedMethod = visitedNode as MethodCallExpression;
        //    if(visitedMethod != null)
        //    {
        //        var source = visitedMethod.Arguments.FirstOrDefault() as MethodCallExpression;
        //        if(source != null)
        //        {
        //            if(source.Method.Name == "Select" && source.Arguments.Count == 2)
        //            {
        //                var newSource = source.Arguments[0];
        //                var projector = source.Arguments[1] as LambdaExpression;
        //                if(projector != null)
        //                {
        //                    var sourceType = projector.Parameters.Select(p => p.Type).FirstOrDefault();
        //                    if(sourceType != null)
        //                    {
        //                        var destType = projector.Body.Type;
        //                        var array = Array.CreateInstance(typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(sourceType, destType)), 1);
        //                        array.SetValue(projector, 0);
        //                        return (Expression) TranslateEnumerableMethodCallInfo.MakeGenericMethod(sourceType, destType).Invoke(null, new object[] { visitedMethod, newSource, array });
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    return visitedNode;
        //}

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
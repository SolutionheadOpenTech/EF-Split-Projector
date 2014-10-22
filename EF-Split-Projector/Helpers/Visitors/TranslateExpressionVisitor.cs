using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class TranslateExpressionVisitor : ExpressionVisitor
    {
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

                return new TranslateExpressionVisitor().FromProjectors(sourceExpression, memberExpressionMappings);
            }

            return sourceExpression;
        }

        private Dictionary<MemberExpression, LambdaExpression> _memberExpressionMappings;
        private bool _rootLambda = true;
        private readonly List<ParameterExpression> _rootParameters = new List<ParameterExpression>();

        private Expression FromProjectors(Expression sourceExpression, Dictionary<MemberExpression, LambdaExpression> memberExpressionMappings)
        {
            if(memberExpressionMappings == null) { throw new ArgumentNullException("memberExpressionMappings"); }
            _memberExpressionMappings = memberExpressionMappings;

            return Visit(sourceExpression);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var visitedExpression = base.VisitLambda(node);
            if(_rootLambda)
            {
                _rootLambda = false;
                var visitedLambda = visitedExpression as LambdaExpression;
                if(visitedLambda != null)
                {
                    return Expression.Lambda(visitedLambda.Body, _rootParameters.Distinct().ToArray());
                }
            }
            return visitedExpression;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            LambdaExpression equivalent;
            if(_memberExpressionMappings.TryGetValue(node, out equivalent))
            {
                _rootParameters.AddRange(equivalent.Parameters);
                return equivalent.Body;
            }
            return base.VisitMember(node);
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class MergeOnProjectorVisitor : ExpressionVisitor
    {
        internal static T MergeOrReplace<T>(params T[] expressions)
            where T : Expression
        {
            if(expressions == null)
            {
                return null;
            }
            
            T rootExpression = null;
            LambdaExpression rootProjector = null;
            var otherProjectors = new List<LambdaExpression>();
            foreach(var expression in expressions)
            {
                if(rootProjector == null)
                {
                    rootProjector = GetFirstMemberInitLambdaVisitor.Get(null, rootExpression = expression);
                }
                else
                {
                    var other = GetFirstMemberInitLambdaVisitor.Get(rootProjector.Body.Type, expression);
                    if(other != null)
                    {
                        otherProjectors.Add(ReplaceParametersVisitor.MergeLambdaParameters(other, rootProjector));
                    }
                }
            }

            if(!otherProjectors.Any())
            {
                return expressions.LastOrDefault();
            }

            return (T) new MergeOnProjectorVisitor(rootProjector, otherProjectors).Visit(rootExpression);
        }

        private readonly LambdaExpression _firstProjector;
        private readonly List<LambdaExpression> _otherProjectors;

        private MergeOnProjectorVisitor(LambdaExpression firstProjector, List<LambdaExpression> otherProjectors)
        {
            _firstProjector = firstProjector;
            _otherProjectors = otherProjectors;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if(node == _firstProjector)
            {
                var mergedMemberInit = _otherProjectors.Aggregate((MemberInitExpression)_firstProjector.Body, (c, o) => MergeMemberInits(c, o.Body as MemberInitExpression));
                return Expression.Lambda<T>(mergedMemberInit, _firstProjector.Parameters);
            }

            return base.VisitLambda<T>(node);
        }

        private static MemberInitExpression MergeMemberInits(MemberInitExpression source, MemberInitExpression other)
        {
            var originalBindings = source.Bindings.ToList();
            var otherBindings = other.Bindings.ToDictionary(b => b.Member, b => b);
            var newBindings = new List<MemberBinding>();

            foreach(var oldBinding in originalBindings)
            {
                Expression mergedAssignment = null;
                var oldAssignment = oldBinding as MemberAssignment;
                if(oldAssignment != null)
                {
                    MemberBinding otherBinding;
                    if(otherBindings.TryGetValue(oldBinding.Member, out otherBinding))
                    {
                        var otherAssignment = otherBinding as MemberAssignment;
                        if(otherAssignment != null)
                        {
                            otherBindings.Remove(oldAssignment.Member);
                            mergedAssignment = MergeOrReplace(oldAssignment.Expression, otherAssignment.Expression);
                            newBindings.Add(Expression.Bind(oldAssignment.Member, mergedAssignment));
                        }
                    }
                }

                if(mergedAssignment == null)
                {
                    newBindings.Add(oldBinding);
                }
            }

            newBindings.AddRange(otherBindings.Values);
            return Expression.MemberInit(source.NewExpression, newBindings);
        }
    }
}
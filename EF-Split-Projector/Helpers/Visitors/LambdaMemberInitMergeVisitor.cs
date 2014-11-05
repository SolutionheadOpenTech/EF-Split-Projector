using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class MergeOnProjectorVisitor : ExpressionVisitor
    {
        public static T Merge<T>(params T[] expressions)
            where T : Expression
        {
            if(expressions == null)
            {
                return null;
            }

            T firstExpression = null;
            LambdaExpression firstProjector = null;
            var otherProjectors = new List<LambdaExpression>();
            foreach(var expression in expressions)
            {
                if(firstProjector == null)
                {
                    firstExpression = expression;
                    firstProjector = GetFirstMemberInitLambdaVisitor.Get(null, expression);
                    if(firstProjector == null)
                    {
                        return firstExpression;
                    }
                }
                else
                {
                    var other = GetFirstMemberInitLambdaVisitor.Get(firstProjector.Body.Type, expression);
                    if(other != null)
                    {
                        otherProjectors.Add(ReplaceParametersVisitor.MergeLambdaParameters(other, firstProjector));
                    }
                }
            }

            if(!otherProjectors.Any())
            {
                return firstExpression;
            }

            return (T) new MergeOnProjectorVisitor(firstProjector, otherProjectors).Visit(firstExpression);
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
                            mergedAssignment = Merge(oldAssignment.Expression, otherAssignment.Expression);
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
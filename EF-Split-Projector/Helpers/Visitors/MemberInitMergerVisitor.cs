using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class MemberInitMergerVisitor : ExpressionVisitor
    {
        public static T MergeLambdasOnMemberInit<T>(IEnumerable<T> lambdas)
            where T : LambdaExpression
        {
            if(lambdas == null)
            {
                return null;
            }

            return MergeMemberInits(lambdas.Aggregate((T) null, (c, n) => c == null ? n : ReplaceParametersVisitor.MergeLambdaParameters(n, c)));
        }

        private static T MergeMemberInits<T>(params T[] expressions)
            where T : Expression
        {
            if(expressions == null)
            {
                return null;
            }

            var others = new List<MemberInitExpression>();
            T first = null;
            foreach(var expression in expressions)
            {
                if(first == null)
                {
                    first = expression;
                }
                else
                {
                    others.Add(GetFirstMemberInitVisitor.Get(null, expression));
                }
            }

            return (T) new MemberInitMergerVisitor(others).Visit(first);
        }

        private readonly List<MemberInitExpression> _others;

        private MemberInitMergerVisitor(IEnumerable<MemberInitExpression> others)
        {
            _others = others.ToList();
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            foreach(var other in _others)
            {
                if(other != null && other.Type == node.Type)
                {
                    var oldBindings = node.Bindings.ToList();
                    var otherBindings = other.Bindings.ToDictionary(b => b.Member, b => b);
                    var newBindings = new List<MemberBinding>();

                    foreach(var oldBinding in oldBindings)
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
                                    mergedAssignment = MergeMemberInits(oldAssignment.Expression, otherAssignment.Expression);
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
                    node = Expression.MemberInit(node.NewExpression, newBindings);
                }
            }

            return node;
        }
    }
}
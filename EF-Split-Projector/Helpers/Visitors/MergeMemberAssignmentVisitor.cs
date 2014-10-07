using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class MergeMemberAssignmentVisitor : ExpressionVisitor
    {
        public static MemberAssignment Merge(MemberAssignment original, MemberAssignment with)
        {
            if(original == null || with == null)
            {
                return original ?? with;
            }

            return new MergeMemberAssignmentVisitor().PrivateMerge(original, with);
        }

        private Expression _with;

        private MemberAssignment PrivateMerge(MemberAssignment original, MemberAssignment with)
        {
            if(original.Member != with.Member) { throw new Exception("MemberInfos don't match."); }

            _with = with.Expression;
            return Expression.Bind(original.Member, Visit(original.Expression));
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            var otherMemberInit = GetFirstMemberInitVisitor.Get(node.Type, _with);
            if(otherMemberInit != null)
            {
                var oldBindings = node.Bindings.ToList();
                var otherBindings = otherMemberInit.Bindings.ToDictionary(a => a.Member, a => a);
                var newBindings = new List<MemberBinding>();
                foreach(var oldBinding in oldBindings)
                {
                    var oldAssignment = oldBinding as MemberAssignment;
                    if(oldAssignment != null)
                    {
                        MemberBinding other;
                        if(otherBindings.TryGetValue(oldAssignment.Member, out other))
                        {
                            newBindings.Add(Merge(oldAssignment, other as MemberAssignment));
                            otherBindings.Remove(oldAssignment.Member);
                            continue;
                        }
                    }
                    
                    newBindings.Add(oldBinding);
                }
                newBindings.AddRange(otherBindings.Values);
                return Expression.MemberInit(node.NewExpression, newBindings);
            }
            return base.VisitMemberInit(node);
        }
    }
}
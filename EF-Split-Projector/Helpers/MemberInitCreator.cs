using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Visitors;

namespace EF_Split_Projector.Helpers
{
    internal class MemberInitCreator
    {
        public static int GetCombinedTotalDepth(params IEnumerable<EntityPathNode>[] entityPaths)
        {
            return entityPaths.SelectMany(p => p)
                              .GroupBy(p => p.NodeKey)
                              .Select(g => g.ToList())
                              .Aggregate(0, (depth, nodes) => depth + (nodes.Count == 1 ?
                                                                           nodes[0].TotalKeyedEntities :
                                                                           ((nodes[0].IsKeyedEntity ? 1 : 0) + GetCombinedTotalDepth(nodes.SelectMany(p => p.Paths).ToArray()))));
        }

        private static List<EntityPathNode> MergePaths(IEnumerable<EntityPathNode> nodes)
        {
            return nodes.GroupBy(n => n.NodeKey)
                        .Select(g => g.ToList())
                        .Select(g => g.Aggregate((EntityPathNode)null, (total, current) => total == null ? current : total.AdoptChildrenOf(current)))
                        .ToList();
        }

        public List<EntityPathNode> EntityPaths { get; private set; }
        private readonly List<MemberAssignment> _memberAssignments = new List<MemberAssignment>();
        private readonly List<MemberBinding> _memberBindings = new List<MemberBinding>();

        public int TotalDepth
        {
            get
            {
                if(_totalDepth == null)
                {
                    _totalDepth = GetCombinedTotalDepth(EntityPaths);
                }
                return _totalDepth.Value;
            }
        }
        private int? _totalDepth;

        public MemberInitCreator(ObjectContext objectContext, params MemberBinding[] memberBindings)
        {
            foreach(var binding in memberBindings)
            {
                var assignment = binding as MemberAssignment;
                if(assignment != null)
                {
                    _memberAssignments.Add(assignment);
                }
                else
                {
                    _memberBindings.Add(binding);
                }
            }

            EntityPaths = MergePaths(_memberAssignments.SelectMany(a => GetEntityPathsVisitor.GetDistinctEntityPaths(objectContext, a.Expression)));
        }

        public MemberInitExpression CreateMemberInit(NewExpression newExpression)
        {
            var bindings = _memberBindings.ToList();
            bindings.AddRange(_memberAssignments);
            return Expression.MemberInit(newExpression, bindings);
        }

        public void MergeWith(MemberInitCreator other)
        {
            var oldAssignments = _memberAssignments.ToList();
            _memberAssignments.Clear();
            var otherAssignments = other._memberAssignments.ToDictionary(a => a.Member, a => a);
            foreach(var memberAssignment in oldAssignments)
            {
                MemberAssignment otherAssignment;
                if(otherAssignments.TryGetValue(memberAssignment.Member, out otherAssignment))
                {
                    _memberAssignments.Add(MergeMemberAssignmentVisitor.Merge(memberAssignment, otherAssignment));
                    otherAssignments.Remove(memberAssignment.Member);
                }
                else
                {
                    _memberAssignments.Add(memberAssignment);
                }
            }
            _memberAssignments.AddRange(otherAssignments.Values);

            _memberBindings.AddRange(other._memberBindings);

            EntityPaths.AddRange(other.EntityPaths);
            EntityPaths = MergePaths(EntityPaths);
            _totalDepth = null;
        }
    }
}
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class ShatterOnMemberInitVisitor : ExpressionVisitor
    {
        public static ShatteredExpression<TExpression> ShatterExpression<TExpression>(TExpression source)
            where TExpression : Expression
        {
            return new ShatteredExpression<TExpression>(source);
        }

        private static ShatteredMemberInit ShatterFirstMemberInit(Expression source)
        {
            var visitor = new ShatterOnMemberInitVisitor();
            visitor.Visit(source);
            return visitor._firstMemberInit;
        }

        private ShatterOnMemberInitVisitor() { }

        private ShatteredMemberInit _firstMemberInit;

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if(_firstMemberInit == null)
            {
                _firstMemberInit = new ShatteredMemberInit(node);
            }
            return base.VisitMemberInit(node);
        }

        public abstract class Shattered<TExpression>
        {
            public TExpression Original { get; private set; }
            public IEnumerable<TExpression> Shards { get { return _shards ?? (_shards = GetShards()); } }
            private IEnumerable<TExpression> _shards;

            protected Shattered(TExpression original)
            {
                Original = original;
            }

            protected abstract IEnumerable<TExpression> GetShards();
        }

        public class ShatteredExpression<TExpression> : Shattered<TExpression>
            where TExpression : Expression
        {
            public readonly ShatteredMemberInit ShatteredMemberInit;

            public IEnumerable<TExpression> MergeShards(ObjectContext objectContext, int prefferedMaxDepth)
            {
                if(ShatteredMemberInit != null)
                {
                    return ConstructExpressions(ShatteredMemberInit.Original, ShatteredMemberInit.MergeShards(objectContext, prefferedMaxDepth));
                }
                return Shards;
            }

            public ShatteredExpression(TExpression original) : base(original)
            {
                ShatteredMemberInit = ShatterFirstMemberInit(original);
            }

            protected override IEnumerable<TExpression> GetShards()
            {
                if(ShatteredMemberInit == null)
                {
                    return new[] { Original };
                }
                return ConstructExpressions(ShatteredMemberInit.Original, ShatteredMemberInit.Shards);
            }

            private IEnumerable<TExpression> ConstructExpressions(MemberInitExpression originalMemberInit, IEnumerable<MemberInitExpression> replacementMemberInits)
            {
                return replacementMemberInits.Select(s => (TExpression)ReplaceBindingsVisitor.ReplaceBindings(Original, originalMemberInit, s.Bindings));
            }
        }

        public class ShatteredMemberInit : Shattered<MemberInitExpression>
        {
            private readonly List<ShatteredMemberBinding> _shatteredBindings;

            public ShatteredMemberInit(MemberInitExpression memberInit) : base(memberInit)
            {
                _shatteredBindings = memberInit.Bindings.Select(b => new ShatteredMemberBinding(b)).ToList();
            }

            public IEnumerable<MemberInitExpression> MergeShards(ObjectContext objectContext, int prefferedMaxDepth)
            {
                var pending = _shatteredBindings
                    .SelectMany(b => b.Shards.Select(s => new MemberInitCreator(objectContext, s)))
                    .OrderBy(b => b.TotalDepth)
                    .ToList();

                var projectors = new List<MemberInitExpression>();
                while(pending.Any())
                {
                    var current = pending[0];
                    pending.RemoveAt(0);
                    var minDepth = current.TotalDepth;

                    var other = pending.ToList().GetEnumerator();
                    while(other.MoveNext() && (current.TotalDepth == minDepth || current.TotalDepth <= prefferedMaxDepth))
                    {
                        var combinedDepth = MemberInitCreator.GetCombinedTotalDepth(current.EntityPaths, other.Current.EntityPaths);
                        if(combinedDepth == minDepth || combinedDepth <= prefferedMaxDepth)
                        {
                            current.MergeWith(other.Current);
                            pending.Remove(other.Current);
                        }
                    }

                    projectors.Add(current.CreateMemberInit(Original.NewExpression));
                }

                return projectors.Any() ? projectors : new[] { Original }.ToList();
            }

            protected override IEnumerable<MemberInitExpression> GetShards()
            {
                return _shatteredBindings.SelectMany(s => s.Shards.Select(b => Expression.MemberInit(Original.NewExpression, b)));
            }

            private class MemberInitCreator
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

        public class ShatteredMemberBinding : Shattered<MemberBinding>
        {
            private readonly MemberAssignment _originalAssignment;
            private readonly ShatteredExpression<Expression> _shatteredAssignment;

            public ShatteredMemberBinding(MemberBinding original) : base(original)
            {
                _originalAssignment = original as MemberAssignment;
                if(_originalAssignment != null)
                {
                    _shatteredAssignment = ShatterExpression(_originalAssignment.Expression);
                }
            }

            protected override IEnumerable<MemberBinding> GetShards()
            {
                if(_shatteredAssignment == null)
                {
                    return new[] { Original };
                }
                return _shatteredAssignment.Shards.Select(s => Expression.Bind(Original.Member, s));
            }
        }
    }
}
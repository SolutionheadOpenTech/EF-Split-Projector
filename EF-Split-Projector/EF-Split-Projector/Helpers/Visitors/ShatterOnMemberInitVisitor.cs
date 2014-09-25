using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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

            public abstract IEnumerable<TExpression> MergeShards(ObjectContext objectContext, int prefferedMaxDepth);

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

            public override IEnumerable<TExpression> MergeShards(ObjectContext objectContext, int prefferedMaxDepth)
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

            public override IEnumerable<MemberInitExpression> MergeShards(ObjectContext objectContext, int prefferedMaxDepth)
            {
                var mergedShards = _shatteredBindings
                    .Select(b => b.MergeShards(objectContext, prefferedMaxDepth).ToList())
                    .ToDictionary(b => b.Select(m => m.Member).Distinct().Single(), b => b.Select(m => new MemberBindingWithPath(m, objectContext)).ToList());
                return MergeMemberBindings(mergedShards);
            }

            protected override IEnumerable<MemberInitExpression> GetShards()
            {
                return _shatteredBindings.SelectMany(s => s.Shards.Select(b => Expression.MemberInit(Original.NewExpression, b)));
            }

            private IEnumerable<MemberInitExpression> MergeMemberBindings(Dictionary<MemberInfo, List<MemberBindingWithPath>> keyedMemberBindings)
            {
                throw new NotImplementedException();
            }

            private class MemberBindingWithPath
            {
                public readonly MemberBinding MemberBinding;
                public readonly List<GetEntityPathsVisitor.EntityPathNode> EntityPaths = new List<GetEntityPathsVisitor.EntityPathNode>();

                public MemberBindingWithPath(MemberBinding memberBinding, ObjectContext objectContext)
                {
                    MemberBinding = memberBinding;
                    var assignment = memberBinding as MemberAssignment;
                    if(assignment != null)
                    {
                        EntityPaths = GetEntityPathsVisitor.GetDistinctEntityPaths(objectContext, assignment.Expression).ToList();
                    }
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

            public override IEnumerable<MemberBinding> MergeShards(ObjectContext objectContext, int prefferedMaxDepth)
            {
                if(_shatteredAssignment == null)
                {
                    return new[] { Original };
                }
                return _shatteredAssignment.MergeShards(objectContext, prefferedMaxDepth).Select(s => Expression.Bind(Original.Member, s));
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
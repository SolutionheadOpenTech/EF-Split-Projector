using System;
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
            return new ShatteredExpression<TExpression>(source, GetDuplicateMemberInitTypesVisitor.GetDuplicateMemberInitTypes(source));
        }

        private ShatterOnMemberInitVisitor(HashSet<Type> immuneToShattering)
        {
            _immuneToShattering = immuneToShattering;
        }

        private readonly HashSet<Type> _immuneToShattering;
        private ShatteredMemberInit _firstMemberInit;

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if(_firstMemberInit == null)
            {
                _firstMemberInit = new ShatteredMemberInit(node, _immuneToShattering);
            }
            return base.VisitMemberInit(node);
        }

        public class ShatteredExpression<TExpression> : ShatteredBase<TExpression>
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

            public ShatteredExpression(TExpression original, HashSet<Type> immuneToShattering) : base(original)
            {
                var visitor = new ShatterOnMemberInitVisitor(immuneToShattering);
                visitor.Visit(original);
                ShatteredMemberInit = visitor._firstMemberInit;
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
    }
}
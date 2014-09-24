using System.Collections.Generic;
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
                return ShatteredMemberInit.Shards.Select(s => (TExpression) ReplaceBindingsVisitor.ReplaceBindings(Original, ShatteredMemberInit.Original, s.Bindings));
            }
        }

        public class ShatteredMemberInit : Shattered<MemberInitExpression>
        {
            private readonly List<ShatteredMemberBinding> _shatteredBindings;

            public ShatteredMemberInit(MemberInitExpression memberInit) : base(memberInit)
            {
                _shatteredBindings = memberInit.Bindings.Select(b => new ShatteredMemberBinding(b)).ToList();
            }

            protected override IEnumerable<MemberInitExpression> GetShards()
            {
                return _shatteredBindings.SelectMany(s => s.Shards.Select(b => Expression.MemberInit(Original.NewExpression, b)));
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
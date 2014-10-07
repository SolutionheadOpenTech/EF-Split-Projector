using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Visitors;

namespace EF_Split_Projector.Helpers
{
    internal class ShatteredMemberBinding : ShatteredBase<MemberBinding>
    {
        private readonly MemberAssignment _originalAssignment;
        private readonly ShatterOnMemberInitVisitor.ShatteredExpression<Expression> _shatteredAssignment;

        public ShatteredMemberBinding(MemberBinding original) : base(original)
        {
            _originalAssignment = original as MemberAssignment;
            if(_originalAssignment != null)
            {
                _shatteredAssignment = ShatterOnMemberInitVisitor.ShatterExpression(_originalAssignment.Expression);
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
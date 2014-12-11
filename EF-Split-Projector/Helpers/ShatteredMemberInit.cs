using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers
{
    internal class ShatteredMemberInit : ShatteredBase<MemberInitExpression>
    {
        private readonly List<ShatteredMemberBinding> _shatteredBindings;

        public ShatteredMemberInit(MemberInitExpression memberInit, ICollection<Type> immuneToShattering) : base(memberInit)
        {
            if(immuneToShattering == null || !immuneToShattering.Contains(memberInit.Type))
            {
                _shatteredBindings = memberInit.Bindings.Select(b => new ShatteredMemberBinding(b)).ToList();
            }
        }

        public IEnumerable<MemberInitExpression> MergeShards(ObjectContextKeys keys, int prefferedMaxDepth)
        {
            if(_shatteredBindings == null)
            {
                return new[] { Original };
            }

            var pending = _shatteredBindings
                .SelectMany(b => b.Shards.Select(s => new MemberInitCreator(keys, s)))
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
            if(_shatteredBindings == null)
            {
                return new[] { Original };
            }

            return _shatteredBindings.SelectMany(s => s.Shards.Select(b => Expression.MemberInit(Original.NewExpression, b)));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class SplitQueryableVisitor : ExpressionVisitor
    {
        public static IEnumerable<IQueryable> Split(IQueryable source, int maxDepth)
        {
            return SplitExpression(source.GetObjectContext(), source.Expression, maxDepth).Select(e => source.Provider.CreateQuery(e));
        }

        public static IEnumerable<IQueryable<T>> Split<T>(IQueryable<T> source, int maxDepth)
        {
            return SplitExpression(source.GetObjectContext(), source.Expression, maxDepth).Select(e => source.Provider.CreateQuery<T>(e));
        }
        
        public static IEnumerable<Expression> SplitExpression(ObjectContext objectContext, Expression source, int maxDepth)
        {
            return new SplitQueryableVisitor(objectContext, maxDepth).SplitFirstMemberInit(source);
        }

        private readonly ObjectContext _objectContext;
        private readonly int _maxDepth;

        private MemberInitExpression _firstMemberInit;
        private IEnumerable<IEnumerable<MemberBinding>> _splitBindings;

        private SplitQueryableVisitor(ObjectContext objectContext, int maxDepth)
        {
            if(objectContext == null) { throw new ArgumentNullException("objectContext"); }
            _objectContext = objectContext;
            _maxDepth = Math.Max(0, maxDepth);
        }

        private IEnumerable<Expression> SplitFirstMemberInit(Expression source)
        {
            Visit(source);

            if(_firstMemberInit != null && _splitBindings != null)
            {
                foreach(var bindings in _splitBindings)
                {
                    yield return ReplaceBindingsVisitor.ReplaceBindings(source, _firstMemberInit, bindings);
                }
            }
            else
            {
                yield return source;
            }
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if(_firstMemberInit == null)
            {
                //var bindings = ShatterOnMemberInitVisitor.ShatterMemberBindings(_objectContext, node, _maxDepth);
                //if(bindings.Count() > 1)
                //{
                //    _firstMemberInit = node;
                //    _splitBindings = bindings;
                //}
            }
            
            return base.VisitMemberInit(node);
        }
    }
}
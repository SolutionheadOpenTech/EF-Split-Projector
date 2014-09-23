using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Extensions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class GetRootedMemberVisitor<TRoot> : ExpressionVisitor
    {
        /// <summary>
        /// Returns a list of all member expressions in the graph that are ultimately rooted in an object of type TRoot.
        /// </summary>
        public static List<MemberExpression> GetRootedMembers(Expression expression)
        {
            var visitor = new GetRootedMemberVisitor<TRoot>();
            visitor.Visit(expression);
            return visitor._memberExpressions.ToList();
        }

        private static readonly Type RootType = typeof(TRoot);
        private readonly Dictionary<MemberExpression, bool> _checkedMemberExpressions = new Dictionary<MemberExpression, bool>();
        private readonly List<MemberExpression> _memberExpressions = new List<MemberExpression>();

        private GetRootedMemberVisitor() { }

        protected override Expression VisitMember(MemberExpression node)
        {
            bool hasBeenChecked;
            if(IsRootedResultMemberExpression(node, out hasBeenChecked))
            {
                if(!hasBeenChecked)
                {
                    _memberExpressions.Add(node);
                }
            }

            return base.VisitMember(node);
        }

        private bool IsRootedResultMemberExpression(MemberExpression memberExpression, out bool alreadyChecked)
        {
            alreadyChecked = false;
            if(memberExpression == null)
            {
                return false;
            }

            bool isRootedResult;
            if(_checkedMemberExpressions.TryGetValue(memberExpression, out isRootedResult))
            {
                alreadyChecked = true;
                return isRootedResult;
            }

            isRootedResult = (memberExpression.Expression != null && memberExpression.Expression.NodeType == ExpressionType.Parameter)
                && memberExpression.Member.DeclaringType.IsOrImplementsType(RootType);
            if(!isRootedResult)
            {
                isRootedResult = IsRootedResultMemberExpression(memberExpression.Expression as MemberExpression, out alreadyChecked);
            }
            _checkedMemberExpressions.Add(memberExpression, isRootedResult);

            return isRootedResult;
        }
    }
}
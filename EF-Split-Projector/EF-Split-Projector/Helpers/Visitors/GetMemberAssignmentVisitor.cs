using System.Collections.Generic;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Extensions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class GetMemberAssignmentVisitor : ExpressionVisitor
    {
        /// <summary>
        /// Returns expression from source that represents the assignment of the member expression.
        /// </summary>
        public static Expression GetMemberAssignment(MemberExpression member, Expression source)
        {
            ParameterExpression memberParameter = null;
            var members = new List<MemberExpression>();
            while(memberParameter == null && member != null)
            {
                members.Insert(0, member);
                memberParameter = member.Expression as ParameterExpression;
                member = member.Expression as MemberExpression;
            }

            var visitor = new GetMemberAssignmentVisitor(memberParameter, members);
            visitor.Visit(source);

            return visitor._equivalentExpression;
        }

        private readonly ParameterExpression _parameterExpression;
        private readonly List<MemberExpression> _memberExpressions;
        private readonly List<ParameterExpression> _parameters = new List<ParameterExpression>();

        private int? _nextMatchedMember;
        private Expression _equivalentExpression;

        private GetMemberAssignmentVisitor(ParameterExpression parameterExpression, List<MemberExpression> memberExpressions)
        {
            _parameterExpression = parameterExpression;
            _memberExpressions = memberExpressions;
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if(_nextMatchedMember == null)
            {
                if(node.Type.IsOrImplementsType(_parameterExpression.Type))
                {
                    _nextMatchedMember = 0;
                }
            }

            return base.VisitMemberInit(node);
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            if(_equivalentExpression == null && _nextMatchedMember != null)
            {
                if(node.Member.Name == _memberExpressions[_nextMatchedMember.Value].Member.Name)
                {
                    _nextMatchedMember++;
                    if(_nextMatchedMember == _memberExpressions.Count)
                    {
                        _equivalentExpression = node.Expression;
                    }
                }
            }

            return base.VisitMemberAssignment(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            _parameters.Add(node);
            return base.VisitParameter(node);
        }
    }
}
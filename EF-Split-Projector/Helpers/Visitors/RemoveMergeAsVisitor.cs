using System.Linq.Expressions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class RemoveMergeAsVisitor : ExpressionVisitor
    {
        public static Expression RemoveMergeAs(Expression expression)
        {
            return new RemoveMergeAsVisitor().Visit(expression);
        }

        private RemoveMergeAsVisitor() { }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            return node.Method.Name == "MergeAs" ? node.Object : base.VisitMethodCall(node);
        }
    }
}
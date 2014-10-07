using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Extensions;

namespace EF_Split_Projector.Helpers.Visitors
{
    internal class GetEntityPathsVisitor : ExpressionVisitor
    {
        public static IEnumerable<EntityPathNode> GetDistinctEntityPaths(ObjectContext objectContext, Expression expression)
        {
            return expression == null ? null : new GetEntityPathsVisitor(objectContext).GatherEntityPaths(expression);
        }

        public static List<EntityPathNode> MergeEntityPathRootNodes(IEnumerable<EntityPathNode> source)
        {
            return source.GroupBy(n => n.NodeKey)
                .Select(g => g.Aggregate((EntityPathNode)null, (s, c) => s == null ? c : s.AdoptChildrenOf(c)))
                .ToList();
        }

        private readonly ObjectContext _objectContext;
        private Dictionary<object, EntityPathNode> _entityPathNodes;

        private GetEntityPathsVisitor(ObjectContext objectContext)
        {
            if(objectContext == null) { throw new ArgumentNullException("objectContext"); }
            _objectContext = objectContext;
        }

        private IEnumerable<EntityPathNode> GatherEntityPaths(Expression expression)
        {
            _entityPathNodes = new Dictionary<object, EntityPathNode>();
            Visit(expression);
            return _entityPathNodes.Values;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            GetOrCreateEntityPathNode(node);
            return base.VisitParameter(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            GetOrCreateEntityPathNode(node);
            return base.VisitMember(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            UpdateMethodCallEntityPaths(node);
            return base.VisitMethodCall(node);
        }

        private EntityPathNode GetOrCreateEntityPathNode(Expression expression)
        {
            if(expression == null)
            {
                return null;
            }

            var member = expression as MemberExpression;
            if(member != null)
            {
                var parentNode = GetOrCreateEntityPathNode(member.Expression);
                if(parentNode == null)
                {
                    return null;
                }
                
                return parentNode.GetOrCreateChildPath(member.Member);
            }

            var method = expression as MethodCallExpression;
            if(method != null)
            {
                return GetOrCreateEntityPathNode(method.Arguments.FirstOrDefault());
            }

            EntityPathNode pathNode;
            if(!_entityPathNodes.TryGetValue(expression, out pathNode))
            {
                pathNode = EntityPathNode.Create(expression, _objectContext);
                if(pathNode != null)
                {
                    _entityPathNodes.Add(expression, pathNode);
                }
            }

            return pathNode;
        }

        private void UpdateMethodCallEntityPaths(MethodCallExpression methodCallExpression)
        {
            var arguments = new Queue<Expression>(methodCallExpression.Arguments);
            if(arguments.Count > 1)
            {
                var firstArgument = arguments.Dequeue();
                var enumeratedEntity = firstArgument.Type.GetEnumerableArgument();
                if(EFHelper.GetKeyProperties(_objectContext, enumeratedEntity) != null)
                {
                    var newPaths = MergeEntityPathRootNodes(arguments.SelectMany(a => GetDistinctEntityPaths(_objectContext, a)));
                    var parent = GetOrCreateEntityPathNode(firstArgument);
                    if(parent != null)
                    {
                        newPaths.RemoveAll(p => parent.AdoptChildrenOf(p) != null);
                    }

                    foreach(var newPath in newPaths)
                    {
                        EntityPathNode existingPath;
                        if(_entityPathNodes.TryGetValue(newPath.NodeKey, out existingPath))
                        {
                            existingPath.AdoptChildrenOf(newPath);
                        }
                        else
                        {
                            _entityPathNodes.Add(newPath.NodeKey, newPath);
                        }
                    }
                }
            }
        }
    }
}
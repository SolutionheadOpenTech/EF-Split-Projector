using System;
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
        private Dictionary<object, EntityPathNode> _methodCallPathRedirects;

        private GetEntityPathsVisitor(ObjectContext objectContext)
        {
            if(objectContext == null) { throw new ArgumentNullException("objectContext"); }
            _objectContext = objectContext;
        }

        private IEnumerable<EntityPathNode> GatherEntityPaths(Expression expression)
        {
            _entityPathNodes = new Dictionary<object, EntityPathNode>();
            _methodCallPathRedirects = new Dictionary<object, EntityPathNode>();
            Visit(expression);
            return _entityPathNodes.Values;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            GetEntityRootNode(node);
            return base.VisitParameter(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            GetEntityPathNode(node);
            return base.VisitMember(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            UpdateMethodCallEntityPaths(node);
            return base.VisitMethodCall(node);
        }

        private EntityPathNode GetEntityPathNode(MemberExpression memberExpression)
        {
            if(memberExpression == null)
            {
                return null;
            }

            EntityPathNode parent = null;

            var parameterParent = memberExpression.Expression as ParameterExpression;
            if(parameterParent != null)
            {
                parent = GetEntityRootNode(parameterParent);
            }
            else
            {
                var memberParent = memberExpression.Expression as MemberExpression;
                if(memberParent != null)
                {
                    parent = GetEntityPathNode(memberParent);
                }
            }

            return parent == null ? null : parent.GetOrCreateChildPath(memberExpression.Member);
        }

        private EntityPathNode GetEntityRootNode(ParameterExpression parameterExpression)
        {
            if(parameterExpression == null)
            {
                return null;
            }

            EntityPathNode pathNode;
            if(!_methodCallPathRedirects.TryGetValue(parameterExpression, out pathNode))
            {
                if(!_entityPathNodes.TryGetValue(parameterExpression, out pathNode))
                {
                    if(EFHelper.GetKeyProperties(_objectContext, parameterExpression.Type) != null)
                    {
                        _entityPathNodes.Add(parameterExpression, pathNode = EntityPathNode.Create(parameterExpression, _objectContext));
                    }
                }
            }

            return pathNode;
        }

        private void UpdateMethodCallEntityPaths(MethodCallExpression methodCallExpression)
        {
            var arguments = methodCallExpression.Arguments.ToList();
            if(arguments.Count > 1)
            {
                var firstArgument = arguments.First();
                var enumeratedEntity = firstArgument.Type.GetEnumerableArgument();
                if(EFHelper.GetKeyProperties(_objectContext, enumeratedEntity) != null)
                {
                    arguments.RemoveAt(0);
                    var newPaths = MergeEntityPathRootNodes(arguments.SelectMany(a => GetDistinctEntityPaths(_objectContext, a)));
                    var parent = GetEntityPathNode(firstArgument as MemberExpression) ?? GetEntityRootNode(firstArgument as ParameterExpression);
                    if(parent != null)
                    {
                        newPaths = RedirectCompatibleNodes(newPaths, parent);
                    }
                    
                    foreach(var newPath in newPaths)
                    {
                        _entityPathNodes.Add(newPath.NodeKey, newPath);
                    }
                }
            }
        }

        private List<EntityPathNode> RedirectCompatibleNodes(IEnumerable<EntityPathNode> sourcePaths, EntityPathNode newParent)
        {
            return sourcePaths.Where(p =>
                {
                    if(newParent.AdoptChildrenOf(p) != null)
                    {
                        if(!_methodCallPathRedirects.ContainsKey(p.NodeKey))
                        {
                            _methodCallPathRedirects.Add(p.NodeKey, newParent);
                        }
                        return false;
                    }
                    return true;
                }).ToList();
        }
    }
}
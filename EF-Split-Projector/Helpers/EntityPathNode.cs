﻿using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EF_Split_Projector.Helpers.Extensions;

namespace EF_Split_Projector.Helpers
{
    internal abstract class EntityPathNode
    {
        public static EntityPathNode Create(Expression expression, ObjectContext objectContext)
        {
            if(expression is ParameterExpression || expression is ConstantExpression)
            {
                return new EntityPathNodeRoot(expression, objectContext);
            }
            return null;
        }

        public EntityPathNode this[MemberInfo member]
        {
            get
            {
                if(member != null)
                {
                    EntityPathNodeMember pathNode;
                    if(_paths.TryGetValue(member, out pathNode))
                    {
                        return pathNode;
                    }
                }

                return null;
            }
        }

        public EntityPathNode Parent
        {
            get { return _parent; }
            set
            {
                _parent = value;
                UpdateString();
            }
        }

        public int TotalKeyedEntities
        {
            get
            {
                if(_totalKeyedEntites == null)
                {
                    _totalKeyedEntites = Paths.Aggregate(IsKeyedEntity ? 1 : 0, (t, n) => t + n.TotalKeyedEntities);
                }
                return _totalKeyedEntites.Value;
            }
        }
        private int? _totalKeyedEntites;

        public IEnumerable<EntityPathNode> Paths { get { return _paths.Values; } }

        public readonly object NodeKey;
        public readonly Type NodeType;
        public readonly bool IsKeyedEntity;

        public EntityPathNode GetOrCreateChildPath(MemberInfo key)
        {
            if(key == null) { return null; }

            var pathNode = this[key];
            if(pathNode == null)
            {
                var memberNode = new EntityPathNodeMember(key, this, _objectContext);
                _paths.Add(key, memberNode);
                _totalKeyedEntites = null;
                pathNode = memberNode;
            }
            return pathNode;
        }

        public EntityPathNode AdoptChildrenOf(EntityPathNode otherParent)
        {
            if(NodeType.GetEnumerableArgument() != otherParent.NodeType && NodeType != otherParent.NodeType)
            {
                return null;
            }

            foreach(var path in otherParent._paths)
            {
                EntityPathNodeMember thisPath;
                if(!_paths.TryGetValue(path.Key, out thisPath))
                {
                    path.Value.Parent = this;
                    _paths.Add(path.Key, path.Value);
                    _totalKeyedEntites = null;
                }
                else
                {
                    thisPath.AdoptChildrenOf(path.Value);
                }
            }

            return this;
        }

        public override string ToString()
        {
            return String;
        }

        #region Private Parts

        protected string String;
        private EntityPathNode _parent;
        private readonly Dictionary<MemberInfo, EntityPathNodeMember> _paths = new Dictionary<MemberInfo, EntityPathNodeMember>();
        private readonly ObjectContext _objectContext;

        private EntityPathNode(object nodeKey, Type nodeType, EntityPathNode parent, ObjectContext objectContext)
        {
            if(nodeKey == null) { throw new ArgumentNullException("nodeKey"); }
            if(nodeType == null) { throw new ArgumentNullException("nodeType"); }
            if(objectContext == null) { throw new ArgumentNullException("objectContext"); }

            NodeKey = nodeKey;
            NodeType = nodeType;
            _objectContext = objectContext;
            Parent = parent;

            IsKeyedEntity = objectContext.GetKeyProperties(nodeType.GetEnumerableArgument() ?? NodeType) != null;
        }

        protected abstract string ConstructString();

        private void UpdateString()
        {
            String = ConstructString();
            foreach(var child in Paths)
            {
                child.UpdateString();
            }
        }

        private sealed class EntityPathNodeMember : EntityPathNode
        {
            public EntityPathNodeMember(MemberInfo memberInfo, EntityPathNode parent, ObjectContext objectContext) : base(memberInfo, memberInfo.GetMemberType(), parent, objectContext) { }

            protected override string ConstructString()
            {
                return String = String.Format("{0}.{1}", Parent, ((MemberInfo)NodeKey).Name);
            }
        }

        private sealed class EntityPathNodeRoot : EntityPathNode
        {
            public EntityPathNodeRoot(Expression constantExpression, ObjectContext objectContext) : base(constantExpression, constantExpression.Type, null, objectContext) { }

            protected override string ConstructString()
            {
                return NodeType.Name;
            }
        }

        #endregion
    }
}
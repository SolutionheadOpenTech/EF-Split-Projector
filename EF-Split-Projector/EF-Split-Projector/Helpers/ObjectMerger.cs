using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EF_Split_Projector.Helpers.Extensions;
using EF_Split_Projector.Helpers.Visitors;

namespace EF_Split_Projector.Helpers
{
    internal abstract class ObjectMerger
    {
        public static ObjectMerger CreateMerger<TSource, TResult>(Expression<Func<TSource, TResult>> projector)
        {
            return CreateMerger(typeof(TResult), projector.Body);
        }

        private static ObjectMerger CreateMerger(Type initType, Expression assignment)
        {
            if(typeof(IEnumerable).IsAssignableFrom(initType))
            {
                return new EnumerableMerger(initType, assignment);
            }

            if(initType.IsComplexType())
            {
                var memberInit = GetFirstMemberInitVisitor.Get(initType, assignment);
                if(memberInit != null)
                {
                    return new ComplexMerger(memberInit.Bindings);
                }
            }

            return null;
        }

        public abstract bool Valid { get; }

        public object Merge(object merge, object mergeWith)
        {
            if(Valid && merge != null && mergeWith != null)
            {
                ValidatedMerge(merge, mergeWith);
            }

            return merge ?? mergeWith;
        }

        protected abstract void ValidatedMerge(object merge, object mergeWith);

        private ObjectMerger() { }

        private sealed class ComplexMerger : ObjectMerger
        {
            public override bool Valid { get { return _memberMergers != null && _memberMergers.Any(); } }

            public ComplexMerger(IEnumerable<MemberBinding> members)
            {
                _memberMergers = members.Select(MemberMerger.GetMerger).Where(p => p.Valid).ToList();
            }

            protected override void ValidatedMerge(object merge, object mergeWith)
            {
                _memberMergers.ForEach(m => m.Merge(merge, mergeWith));
            }

            private readonly List<MemberMerger> _memberMergers;
        }

        private sealed class EnumerableMerger : ObjectMerger
        {
            public EnumerableMerger(Type type, Expression assignment)
            {
                var enumerable = typeof(IEnumerable<>);
                var interfaces = type.GetInterfaces().ToList();
                if(type.IsInterface && type.IsGenericType)
                {
                    interfaces.Add(type);
                }

                var typedEnumerable = type.GetGenericInterfaceImplementation(enumerable);
                if(typedEnumerable != null)
                {
                    var genericArgument = typedEnumerable.GetGenericArguments().Single();
                    _elementMerger = CreateMerger(genericArgument, assignment);
                }
            }

            private readonly ObjectMerger _elementMerger;

            public override bool Valid { get { return _elementMerger != null && _elementMerger.Valid; } }

            protected override void ValidatedMerge(object merge, object mergeWith)
            {
                var enumerator0 = ((IEnumerable)merge).GetEnumerator();
                var enumerator1 = ((IEnumerable)mergeWith).GetEnumerator();
                while(enumerator0.MoveNext() && enumerator1.MoveNext())
                {
                    _elementMerger.Merge(enumerator0.Current, enumerator1.Current);
                }
            }
        }

        private abstract class MemberMerger : ObjectMerger
        {
            public static MemberMerger GetMerger(MemberBinding memberBinding)
            {
                if(memberBinding == null) { throw new ArgumentNullException("memberBinding"); }

                var assignment = memberBinding as MemberAssignment;
                if(assignment == null)
                {
                    throw new Exception(String.Format("memberBinding must be MemberAssignment expression."));
                }

                if(assignment.Member is PropertyInfo)
                {
                    return new PropertyMemberMerger(assignment);
                }

                if(assignment.Member is FieldInfo)
                {
                    return new FieldMemberMerger(assignment);
                }

                throw new NotSupportedException(String.Format("Cannot create MemberMerger from MemberInfo of type '{0}'.", assignment.Member.GetType().Name));
            }

            private ObjectMerger _memberValueMerger;
            private readonly Expression _assignment;

            private MemberMerger(Expression assignment)
            {
                _assignment = assignment;
            }

            private void Initialize()
            {
                if(Valid)
                {
                    var memberValueMerger = CreateMerger(MemberType, _assignment);
                    if(memberValueMerger != null && memberValueMerger.Valid)
                    {
                        _memberValueMerger = memberValueMerger;
                    }
                }
            }

            protected sealed override void ValidatedMerge(object merge, object mergeWith)
            {
                var otherMember = GetMember(mergeWith);
                SetMember(merge, _memberValueMerger != null ? _memberValueMerger.Merge(GetMember(merge), otherMember) : otherMember);
            }

            protected abstract Type MemberType { get; }
            protected abstract object GetMember(object source);
            protected abstract void SetMember(object source, object member);

            private sealed class PropertyMemberMerger : MemberMerger
            {
                public override bool Valid { get { return _valid; } }
                protected override Type MemberType { get { return _propertyInfo.PropertyType; } }

                private readonly PropertyInfo _propertyInfo;
                private readonly MethodInfo _get;
                private readonly MethodInfo _set;
                private readonly bool _valid;

                public PropertyMemberMerger(MemberAssignment assignment)
                    : base(assignment.Expression)
                {
                    _propertyInfo = assignment.Member as PropertyInfo;
                    if(_propertyInfo != null)
                    {
                        _get = _propertyInfo.GetGetMethod() ?? _propertyInfo.GetGetMethod(true);
                        _set = _propertyInfo.GetSetMethod() ?? _propertyInfo.GetSetMethod(true);
                        _valid = _get != null && _set != null;
                    }
                    else
                    {
                        _valid = false;
                    }

                    Initialize();
                }

                protected override object GetMember(object source)
                {
                    return _get.Invoke(source, null);
                }

                protected override void SetMember(object source, object member)
                {
                    _set.Invoke(source, new[] { member });
                }
            }

            private sealed class FieldMemberMerger : MemberMerger
            {
                public override bool Valid { get { return _fieldInfo != null; } }
                protected override Type MemberType { get { return _fieldInfo.FieldType; } }
                private readonly FieldInfo _fieldInfo;

                public FieldMemberMerger(MemberAssignment memberAssignment)
                    : base(memberAssignment.Expression)
                {
                    _fieldInfo = memberAssignment.Member as FieldInfo;
                    Initialize();
                }

                protected override object GetMember(object source)
                {
                    return _fieldInfo.GetValue(source);
                }

                protected override void SetMember(object source, object member)
                {
                    _fieldInfo.SetValue(source, member);
                }
            }
        }
    }
}
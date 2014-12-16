using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Split_Projector
{
    public abstract class SplitQueryableBase : IOrderedQueryable, IDbAsyncEnumerable
    {
        public abstract Expression Expression { get; }
        public abstract Type ElementType { get; }
        public abstract IQueryProvider Provider { get; }

        public string CommandString { get { return _commandString ?? (_commandString = GetCommandString()); } }
        private string _commandString;

        public IQueryable InternalQuery { get { return _InternalQuery; } }
        protected IQueryable _InternalQuery;

        public List<Func<IQueryable, object>> InternalDelegates { get { return _InternalDelegates; } }
        protected List<Func<IQueryable, object>> _InternalDelegates;
        
        protected abstract IEnumerator InternalGetEnumerator();
        protected abstract IDbAsyncEnumerator InternalGetAsyncEnumerator();
        protected abstract string GetCommandString();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return InternalGetEnumerator();
        }

        IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator()
        {
            return InternalGetAsyncEnumerator();
        }

        internal SplitQueryableBase() { }
    }
}
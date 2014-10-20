using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace EF_Split_Projector
{
    internal class SplitQueryDbAsyncEnumerator<TSource, TProjection, TResult> : IDbAsyncEnumerator<TResult>
    {
        object IDbAsyncEnumerator.Current { get { return Current; } }

        public TResult Current { get { return GetEnumerator().Current; } }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => GetEnumerator().MoveNext(), cancellationToken);
        }

        public void Dispose()
        {
            if(_enumerator != null)
            {
                _enumerator.Dispose();
                _enumerator = null;
            }
        }

        #region Private Internal

        private readonly SplitQueryable<TSource, TProjection, TResult> _source;
        private IEnumerator<TResult> _enumerator;

        internal SplitQueryDbAsyncEnumerator(SplitQueryable<TSource, TProjection, TResult> source)
        {
            _source = source;
        }

        private IEnumerator<TResult> GetEnumerator()
        {
            return (_enumerator ?? (_enumerator = _source.GetEnumerator()));
        }

        #endregion
    }
}
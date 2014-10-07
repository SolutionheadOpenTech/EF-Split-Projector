using System.Collections.Generic;

namespace EF_Split_Projector.Helpers
{
    internal abstract class ShatteredBase<T>
    {
        public T Original { get; private set; }
        public IEnumerable<T> Shards { get { return _shards ?? (_shards = GetShards()); } }
        private IEnumerable<T> _shards;

        protected ShatteredBase(T original)
        {
            Original = original;
        }

        protected abstract IEnumerable<T> GetShards();
    }
}
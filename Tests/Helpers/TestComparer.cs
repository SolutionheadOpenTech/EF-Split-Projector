using System.Collections.Generic;

namespace Tests.Helpers
{
    public class TestComparer<T> : IEqualityComparer<T>, IComparer<T>
    {
        public bool Equals(T x, T y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }

        public int Compare(T x, T y)
        {
            var x2 = x as double?;
            var y2 = y as double?;
            if(x2 != null & y2 != null)
            {
                return (int) (x2.Value - y2.Value);
            }

            return -1;
        }
    }
}
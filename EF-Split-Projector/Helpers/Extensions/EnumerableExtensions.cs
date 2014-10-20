using System.Collections.Generic;
using System.Linq;

namespace EF_Split_Projector.Helpers.Extensions
{
    internal static class EnumerableExtensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source.Distinct());
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    public static class EquivalentHelper
    {
        public static bool AreEquivalent(object expected, object result)
        {
            if(expected == null)
            {
                return result == null;
            }

            if(result == null)
            {
                return false;
            }

            var expectedEnumerable = expected as IEnumerable;
            if(expectedEnumerable != null)
            {
                return AreEquivalent(((IEnumerable)expected).Cast<object>().ToList(), ((IEnumerable)result).Cast<object>().ToList());
            }

            var expectedType = expected.GetType();
            var resultType = result.GetType();

            var expectedProperties = expectedType.GetProperties()
                                                 .Select(p => p.GetGetMethod())
                                                 .Where(m => m != null).ToList();
            var resultProperties = expectedProperties.Select(e => resultType.GetProperty(e.Name))
                                                     .Where(p => p != null)
                                                     .Select(p => p.GetGetMethod())
                                                     .Where(m => m != null).ToDictionary(m => m.Name, m => m);
            if(expectedProperties.Count != resultProperties.Count)
            {
                return false;
            }
            if(expectedProperties.Any(e => !AreEquivalent(e.Invoke(expected, null), resultProperties[e.Name].Invoke(result, null))))
            {
                return false;
            }

            var expectedFields = expectedType.GetFields().ToList();
            var resultFields = expectedFields.Select(e => resultType.GetField(e.Name))
                                             .Where(p => p != null).ToDictionary(m => m.Name, m => m);
            if(expectedFields.Count != resultFields.Count)
            {
                return false;
            }
            return expectedFields.All(e => AreEquivalent(e.GetValue(expected), resultFields[e.Name].GetValue(result)));
        }

        private static bool AreEquivalent(IReadOnlyCollection<object> expected, IReadOnlyCollection<object> result)
        {
            if(expected.Count != result.Count)
            {
                return false;
            }

            return !expected.Any(e => result.All(r => !AreEquivalent(e, r)));
        }
    }
}
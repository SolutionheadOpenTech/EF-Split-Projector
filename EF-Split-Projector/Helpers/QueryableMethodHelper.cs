using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EF_Split_Projector.Helpers
{
    internal static class QueryableMethodHelper<TElement>
    {
        public static List<MethodInfoWithParameters> GetMethods(string methodName)
        {
            List<MethodInfoWithParameters> methods;
            return Methods.TryGetValue(methodName.ToUpper(), out methods) ? methods.ToList() : null;
        }

        public static MethodInfo GetMethod(string methodName, params Type[] parameters)
        {
            var methods = GetMethods(methodName);
            if(methods != null)
            {
                var match = methods.FirstOrDefault(m => m.ParametersMatch(parameters));
                return match != null ? match.MethodInfo : null;
            }
            return null;
        }

        private static readonly Dictionary<string, List<MethodInfoWithParameters>> Methods = typeof(Queryable).GetMethods()
                                                                                  .Where(m => m.IsGenericMethodDefinition && m.GetGenericArguments().Count() == 1)
                                                                                  .Select(m => m.MakeGenericMethod(typeof(TElement)))
                                                                                  .GroupBy(m => m.Name.ToUpper()).ToDictionary(g => g.Key, g => g.Select(m => new MethodInfoWithParameters(m)).ToList());

        public class MethodInfoWithParameters
        {
            public readonly MethodInfo MethodInfo;
            public readonly List<Type> Parameters;

            public MethodInfoWithParameters(MethodInfo methodInfo)
            {
                MethodInfo = methodInfo;
                Parameters = MethodInfo.GetParameters().Select(p => p.ParameterType).ToList();
            }

            public bool ParametersMatch(params Type[] parameters)
            {
                if(parameters == null)
                {
                    return Parameters.Any();
                }

                var count = parameters.Count();
                if(count != Parameters.Count)
                {
                    return false;
                }

                for(var i = 0; i < count; ++i)
                {
                    var parameter = parameters[i];
                    if(parameter != null && parameter != Parameters[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}

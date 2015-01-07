using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EF_Split_Projector.Helpers.Extensions
{
    public static class MethodInfoExtensions
    {
        public static MethodInfo UpdateGenericArguments(this MethodInfo methodInfo, IEnumerable<Type> replaceParameterTypes)
        {
            if(methodInfo == null || (!methodInfo.IsGenericMethod && !methodInfo.IsGenericMethodDefinition))
            {
                return null;
            }

            var genericMethodDefinition = methodInfo.IsGenericMethodDefinition ? methodInfo : methodInfo.GetGenericMethodDefinition();
            var genericArguments = genericMethodDefinition.GetGenericArguments().ToList();
            var genericParameters = genericMethodDefinition.GetParameters().Select(p => p.ParameterType).ToList();
            var newParameters = replaceParameterTypes.ToList();

            var newArguments = genericArguments.Select(g => SearchForGenericType(g, genericParameters, newParameters)).ToArray();
            return newArguments.Any(a => a == null) ? null : genericMethodDefinition.MakeGenericMethod(newArguments);
        }

        private static Type SearchForGenericType(Type genericArgument, List<Type> parameterDefinitions, List<Type> newParameters)
        {
            if(parameterDefinitions.Count == newParameters.Count)
            {
                foreach(var parameter in parameterDefinitions.Zip(newParameters, (f, s) => new
                    {
                        Definition = f,
                        New = s
                    }))
                {
                    Type match = null;

                    if(parameter.Definition == genericArgument)
                    {
                        match = parameter.New;
                    }
                    else if(parameter.Definition.IsGenericType && parameter.New.IsGenericType)
                    {
                        var definition = parameter.Definition.GetGenericTypeDefinition();

                        Type newArgumentSource;
                        if(parameter.Definition.IsInterface)
                        {
                            var interfaces = parameter.New.GetInterfaces().ToList();
                            if(parameter.New.IsInterface)
                            {
                                interfaces.Add(parameter.New);
                            }
                            newArgumentSource = interfaces.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == definition);
                        }
                        else
                        {
                            newArgumentSource = parameter.New;
                            while(newArgumentSource != null && !(newArgumentSource.IsGenericType && newArgumentSource.GetGenericTypeDefinition() == definition))
                            {
                                newArgumentSource = newArgumentSource.BaseType;
                            }
                        }

                        if(newArgumentSource != null)
                        {
                            var genericArguments = parameter.Definition.GetGenericArguments().ToList();
                            var newArguments = newArgumentSource.GetGenericArguments().ToList();
                            match = SearchForGenericType(genericArgument, genericArguments, newArguments);
                        }
                    }

                    if(match != null)
                    {
                        return match;
                    }
                }
            }

            return null;
        }
    }
}
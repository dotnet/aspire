// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Aspire.Hosting.RemoteHost;

/// <summary>
/// Finds the best matching method or constructor for given arguments.
/// Encapsulates the complex scoring and matching logic.
/// </summary>
internal sealed class MethodResolver
{
    /// <summary>
    /// Finds the best matching method from candidates by scoring argument names against parameter names.
    /// </summary>
    /// <param name="candidates">The candidate methods to consider.</param>
    /// <param name="args">The arguments provided by the caller.</param>
    /// <param name="skipParameters">Number of parameters to skip (e.g., 1 for extension methods to skip 'this').</param>
    /// <returns>The best matching method, or null if no suitable match is found.</returns>
    public static MethodInfo? FindBestMethod(
        IEnumerable<MethodInfo> candidates,
        IReadOnlyDictionary<string, JsonElement> args,
        int skipParameters = 0)
    {
        return candidates
            .Select(m => (Method: m, Score: ScoreMethod(m, args, skipParameters)))
            .Where(x => x.Score > int.MinValue)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Method.GetParameters().Length) // Prefer simpler methods if tied
            .Select(x => x.Method)
            .FirstOrDefault();
    }

    /// <summary>
    /// Finds the best matching constructor by scoring argument names.
    /// </summary>
    /// <param name="type">The type to find a constructor for.</param>
    /// <param name="args">The arguments provided by the caller.</param>
    /// <returns>The best matching constructor, or null if no suitable match is found.</returns>
    public static ConstructorInfo? FindBestConstructor(
        Type type,
        IReadOnlyDictionary<string, JsonElement> args)
    {
        return type.GetConstructors()
            .Select(c => (Ctor: c, Score: ScoreMethod(c, args, 0)))
            .OrderByDescending(x => x.Score)
            .Select(x => x.Ctor)
            .FirstOrDefault();
    }

    /// <summary>
    /// Scores a method based on how well the provided args match parameter names.
    /// +10 for each matching arg name, -100 for each missing required param.
    /// </summary>
    /// <param name="method">The method or constructor to score.</param>
    /// <param name="args">The arguments provided by the caller.</param>
    /// <param name="skipParameters">Number of parameters to skip when scoring.</param>
    /// <returns>The score (higher is better).</returns>
    public static int ScoreMethod(MethodBase method, IReadOnlyDictionary<string, JsonElement> args, int skipParameters = 0)
    {
        var parameters = method.GetParameters().Skip(skipParameters).ToList();
        var paramNames = parameters.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var score = 0;

        // Award points for each provided arg that matches a parameter name
        foreach (var argName in args.Keys)
        {
            if (paramNames.Contains(argName))
            {
                score += 10;
            }
        }

        // Penalize for each required parameter that wasn't provided
        foreach (var param in parameters)
        {
            if (!param.HasDefaultValue && !args.ContainsKey(param.Name!))
            {
                score -= 100;
            }
        }

        return score;
    }

    /// <summary>
    /// Gets static methods from a type, optionally including extension methods.
    /// </summary>
    /// <param name="type">The type to get methods from.</param>
    /// <param name="methodName">The method name to match.</param>
    /// <param name="includeExtensions">Whether to include extension methods.</param>
    /// <returns>Matching methods.</returns>
    public static IEnumerable<MethodInfo> GetStaticMethods(Type type, string methodName, bool includeExtensions = true)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));

        if (!includeExtensions)
        {
            methods = methods.Where(m => !m.IsDefined(typeof(ExtensionAttribute), false));
        }

        return methods;
    }

    /// <summary>
    /// Gets instance methods from a type.
    /// </summary>
    /// <param name="type">The type to get methods from.</param>
    /// <param name="methodName">The method name to match.</param>
    /// <returns>Matching methods.</returns>
    public static IEnumerable<MethodInfo> GetInstanceMethods(Type type, string methodName)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a method is compatible with a source object type for extension method calls.
    /// </summary>
    /// <param name="method">The extension method to check.</param>
    /// <param name="sourceType">The type of the source object.</param>
    /// <returns>True if compatible, false otherwise.</returns>
    public static bool IsExtensionMethodCompatible(MethodInfo method, Type sourceType)
    {
        if (!method.IsDefined(typeof(ExtensionAttribute), false))
        {
            return false;
        }

        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            return false;
        }

        var firstParamType = parameters[0].ParameterType;

        // Handle generic parameter types
        if (firstParamType.IsGenericType)
        {
            var genericTypeDef = firstParamType.GetGenericTypeDefinition();
            return sourceType.GetInterfaces().Any(iface =>
                iface.IsGenericType && iface.GetGenericTypeDefinition() == genericTypeDef);
        }

        return firstParamType.IsAssignableFrom(sourceType);
    }

    /// <summary>
    /// Makes a generic method concrete by inferring type arguments from actual argument values.
    /// </summary>
    /// <param name="method">The generic method definition.</param>
    /// <param name="arguments">The actual argument values.</param>
    /// <returns>The concrete method with type arguments resolved.</returns>
    public static MethodInfo MakeGenericMethodFromArgs(MethodInfo method, object?[] arguments)
    {
        if (!method.ContainsGenericParameters)
        {
            return method;
        }

        var genericArgs = method.GetGenericArguments();
        var resolvedTypes = new Type[genericArgs.Length];
        var parameters = method.GetParameters();

        // Infer each generic argument from the actual argument types
        for (var i = 0; i < genericArgs.Length; i++)
        {
            resolvedTypes[i] = InferGenericArgument(genericArgs[i], parameters, arguments) ?? typeof(object);
        }

        return method.MakeGenericMethod(resolvedTypes);
    }

    private static Type? InferGenericArgument(Type genericArg, ParameterInfo[] parameters, object?[] arguments)
    {
        for (var j = 0; j < parameters.Length && j < arguments.Length; j++)
        {
            var paramType = parameters[j].ParameterType;
            var argument = arguments[j];

            if (argument == null)
            {
                continue;
            }

            // Direct match: parameter is the generic argument itself (e.g., T param)
            if (paramType == genericArg)
            {
                return argument.GetType();
            }

            // Generic type match: extract from IResourceBuilder<T>, etc.
            if (paramType.IsGenericType)
            {
                var typeArgs = paramType.GetGenericArguments();
                for (var k = 0; k < typeArgs.Length; k++)
                {
                    if (typeArgs[k] == genericArg)
                    {
                        var argType = argument.GetType();

                        // Try direct generic type match
                        if (argType.IsGenericType)
                        {
                            return argType.GetGenericArguments()[k];
                        }

                        // Try to find matching interface
                        var iface = argType.GetInterfaces()
                            .FirstOrDefault(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == paramType.GetGenericTypeDefinition());

                        if (iface != null)
                        {
                            return iface.GetGenericArguments()[k];
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Parses JSON arguments into a dictionary.
    /// </summary>
    /// <param name="args">The JSON element containing arguments.</param>
    /// <returns>A dictionary of argument names to values.</returns>
    public static IReadOnlyDictionary<string, JsonElement> ParseArgs(JsonElement? args)
    {
        var result = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        if (args.HasValue && args.Value.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in args.Value.EnumerateObject())
            {
                result[prop.Name] = prop.Value;
            }
        }

        return result;
    }
}

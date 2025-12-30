// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;

namespace Aspire.Hosting.RemoteHost;

/// <summary>
/// Resolves types from assembly and type names.
/// Single source of truth for type/assembly resolution - replaces duplicated patterns.
/// </summary>
internal sealed class TypeResolver
{
    private readonly ConcurrentDictionary<string, Assembly> _assemblyCache = new();

    /// <summary>
    /// Resolves a type from assembly and type names.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly containing the type.</param>
    /// <param name="typeName">The fully qualified type name.</param>
    /// <returns>The resolved type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the assembly or type cannot be found.</exception>
    public Type ResolveType(string assemblyName, string typeName)
    {
        var assembly = ResolveAssembly(assemblyName);
        return assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' not found in assembly '{assemblyName}'");
    }

    /// <summary>
    /// Resolves an assembly by name, using caching for efficiency.
    /// Requires the assembly to be loadable by name - does not scan loaded assemblies.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly to resolve.</param>
    /// <returns>The resolved assembly.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the assembly cannot be found.</exception>
    public Assembly ResolveAssembly(string assemblyName)
    {
        return _assemblyCache.GetOrAdd(assemblyName, name =>
        {
            try
            {
                return Assembly.Load(name);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Assembly '{name}' not found", ex);
            }
        });
    }

    /// <summary>
    /// Clears the assembly cache.
    /// </summary>
    public void Clear()
    {
        _assemblyCache.Clear();
    }
}

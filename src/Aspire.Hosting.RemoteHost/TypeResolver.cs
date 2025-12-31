// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;

namespace Aspire.Hosting.RemoteHost;

/// <summary>
/// Resolves types from assembly and type names with security filtering.
/// Only allows access to assemblies matching the security policy allowlist.
/// </summary>
internal sealed class TypeResolver
{
    private readonly ConcurrentDictionary<string, Assembly> _assemblyCache = new();
    private readonly SecurityPolicy _securityPolicy;

    public TypeResolver() : this(SecurityPolicy.Default)
    {
    }

    public TypeResolver(SecurityPolicy securityPolicy)
    {
        _securityPolicy = securityPolicy;
    }

    /// <summary>
    /// Resolves a type from assembly and type names.
    /// Validates that the assembly is allowed by the security policy.
    /// Blocked assemblies are treated like unfound types - logged and rejected.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly containing the type.</param>
    /// <param name="typeName">The fully qualified type name.</param>
    /// <returns>The resolved type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the assembly is blocked, not found, or type cannot be found.</exception>
    public Type ResolveType(string assemblyName, string typeName)
    {
        // Security check: treat blocked assemblies like unfound types
        // Log for server-side audit, but return generic error to untrusted caller
        if (!_securityPolicy.IsAssemblyAllowed(assemblyName))
        {
            Console.WriteLine($"[RPC] SECURITY: Blocked access to assembly '{assemblyName}' - not in allowlist");
            throw new InvalidOperationException($"Assembly '{assemblyName}' not found");
        }

        var assembly = ResolveAssembly(assemblyName);
        return assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' not found in assembly '{assemblyName}'");
    }

    /// <summary>
    /// Resolves an assembly by name, using caching for efficiency.
    /// Does NOT validate security policy - use ResolveType for external calls.
    /// </summary>
    private Assembly ResolveAssembly(string assemblyName)
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

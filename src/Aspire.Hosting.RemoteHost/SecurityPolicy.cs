// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1822 // Mark members as static - instance methods allow for extensibility

namespace Aspire.Hosting.RemoteHost;

/// <summary>
/// Security policy for the RPC layer. Controls which assemblies and types
/// can be accessed via RPC calls.
/// </summary>
internal sealed class SecurityPolicy
{
    /// <summary>
    /// Assembly prefixes that are allowed to be accessed via RPC.
    /// Only assemblies whose names start with one of these prefixes can be used
    /// in createObject, invokeStaticMethod, getStaticProperty, and setStaticProperty.
    /// </summary>
    private static readonly string[] s_allowedAssemblyPrefixes =
    [
        "Aspire.Hosting",
        "Microsoft.Extensions",
    ];

    /// <summary>
    /// Gets the list of allowed assembly prefixes.
    /// </summary>
    public static IReadOnlyList<string> AllowedAssemblyPrefixes => s_allowedAssemblyPrefixes;

    /// <summary>
    /// Singleton instance with default policy.
    /// </summary>
    public static SecurityPolicy Default { get; } = new();

    /// <summary>
    /// Validates that an assembly is allowed to be accessed via RPC.
    /// </summary>
    /// <param name="assemblyName">The assembly name to validate.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if the assembly is not allowed.</exception>
    public void ValidateAssemblyAccess(string assemblyName)
    {
        if (!IsAssemblyAllowed(assemblyName))
        {
            Console.WriteLine($"[RPC] SECURITY: Blocked access to assembly '{assemblyName}' - not in allowlist");
            throw new UnauthorizedAccessException("Access denied.");
        }
    }

    /// <summary>
    /// Checks if an assembly is allowed to be accessed via RPC.
    /// </summary>
    /// <param name="assemblyName">The assembly name to check.</param>
    /// <returns>True if allowed, false otherwise.</returns>
    public bool IsAssemblyAllowed(string assemblyName)
    {
        foreach (var prefix in s_allowedAssemblyPrefixes)
        {
            if (assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

}

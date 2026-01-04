// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Reflection;

namespace Aspire.Hosting.Ats;

/// <summary>
/// Provides CLR type to ATS type ID mapping based on <see cref="AspireExportAttribute"/> declarations.
/// </summary>
/// <remarks>
/// <para>
/// This class centralizes all type mapping logic, replacing scattered inference and string parsing.
/// It scans assemblies for <see cref="AspireExportAttribute"/> with <see cref="AspireExportAttribute.AtsTypeId"/>
/// set on types or at assembly level.
/// </para>
/// <para>
/// The mapping is used by:
/// <list type="bullet">
/// <item><description>Code generation (to generate correct TypeScript/Python types)</description></item>
/// <item><description>Runtime dispatcher (to validate handle types)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class AtsTypeMapping
{
    private readonly FrozenDictionary<string, string> _fullNameToTypeId;
    private readonly FrozenDictionary<string, string> _typeIdToFullName;

    /// <summary>
    /// Gets an empty type mapping with no registered types.
    /// </summary>
    public static AtsTypeMapping Empty { get; } = new AtsTypeMapping(
        FrozenDictionary<string, string>.Empty,
        FrozenDictionary<string, string>.Empty);

    private AtsTypeMapping(
        FrozenDictionary<string, string> fullNameToTypeId,
        FrozenDictionary<string, string> typeIdToFullName)
    {
        _fullNameToTypeId = fullNameToTypeId;
        _typeIdToFullName = typeIdToFullName;
    }

    /// <summary>
    /// Creates a type mapping by scanning the specified assemblies for <see cref="AspireExportAttribute"/> declarations.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>A type mapping containing all discovered type mappings.</returns>
    public static AtsTypeMapping FromAssemblies(IEnumerable<Assembly> assemblies)
    {
        var fullNameToTypeId = new Dictionary<string, string>(StringComparer.Ordinal);
        var typeIdToFullName = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var assembly in assemblies)
        {
            ScanAssembly(assembly, fullNameToTypeId, typeIdToFullName);
        }

        return new AtsTypeMapping(
            fullNameToTypeId.ToFrozenDictionary(StringComparer.Ordinal),
            typeIdToFullName.ToFrozenDictionary(StringComparer.Ordinal));
    }

    /// <summary>
    /// Creates a type mapping by scanning the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>A type mapping containing all discovered type mappings.</returns>
    public static AtsTypeMapping FromAssembly(Assembly assembly)
    {
        return FromAssemblies([assembly]);
    }

    private static void ScanAssembly(
        Assembly assembly,
        Dictionary<string, string> fullNameToTypeId,
        Dictionary<string, string> typeIdToFullName)
    {
        // Scan assembly-level attributes
        foreach (var attr in assembly.GetCustomAttributes<AspireExportAttribute>())
        {
            if (attr.Type != null && !string.IsNullOrEmpty(attr.AtsTypeId))
            {
                var fullName = attr.Type.FullName;
                if (fullName != null)
                {
                    fullNameToTypeId[fullName] = attr.AtsTypeId;
                    typeIdToFullName[attr.AtsTypeId] = fullName;
                }
            }
        }

        // Scan type-level attributes
        try
        {
            foreach (var type in assembly.GetTypes())
            {
                var attr = type.GetCustomAttribute<AspireExportAttribute>();
                if (attr != null && !string.IsNullOrEmpty(attr.AtsTypeId))
                {
                    var fullName = type.FullName;
                    if (fullName != null)
                    {
                        fullNameToTypeId[fullName] = attr.AtsTypeId;
                        typeIdToFullName[attr.AtsTypeId] = fullName;
                    }
                }
            }
        }
        catch (ReflectionTypeLoadException)
        {
            // Skip assemblies that can't be fully loaded
        }
    }

    /// <summary>
    /// Gets the ATS type ID for a CLR type.
    /// </summary>
    /// <param name="type">The CLR type.</param>
    /// <returns>The ATS type ID, or null if not mapped.</returns>
    public string? GetTypeId(Type type)
    {
        return type.FullName != null && _fullNameToTypeId.TryGetValue(type.FullName, out var typeId)
            ? typeId
            : null;
    }

    /// <summary>
    /// Gets the ATS type ID for a CLR type by full name.
    /// </summary>
    /// <param name="fullName">The full name of the CLR type.</param>
    /// <returns>The ATS type ID, or null if not mapped.</returns>
    public string? GetTypeId(string fullName)
    {
        return _fullNameToTypeId.TryGetValue(fullName, out var typeId) ? typeId : null;
    }

    /// <summary>
    /// Gets the CLR type full name for an ATS type ID.
    /// </summary>
    /// <param name="typeId">The ATS type ID.</param>
    /// <returns>The CLR type full name, or null if not mapped.</returns>
    public string? GetFullName(string typeId)
    {
        return _typeIdToFullName.TryGetValue(typeId, out var fullName) ? fullName : null;
    }

    /// <summary>
    /// Tries to get the ATS type ID for a CLR type.
    /// </summary>
    /// <param name="type">The CLR type.</param>
    /// <param name="typeId">The ATS type ID if found.</param>
    /// <returns>True if the type was found in the mapping.</returns>
    public bool TryGetTypeId(Type type, out string? typeId)
    {
        if (type.FullName != null && _fullNameToTypeId.TryGetValue(type.FullName, out var id))
        {
            typeId = id;
            return true;
        }
        typeId = null;
        return false;
    }

    /// <summary>
    /// Tries to get the ATS type ID for a CLR type by full name.
    /// </summary>
    /// <param name="fullName">The full name of the CLR type.</param>
    /// <param name="typeId">The ATS type ID if found.</param>
    /// <returns>True if the type was found in the mapping.</returns>
    public bool TryGetTypeId(string fullName, out string? typeId)
    {
        if (_fullNameToTypeId.TryGetValue(fullName, out var id))
        {
            typeId = id;
            return true;
        }
        typeId = null;
        return false;
    }

    /// <summary>
    /// Gets all registered type IDs.
    /// </summary>
    public IEnumerable<string> TypeIds => _typeIdToFullName.Keys;

    /// <summary>
    /// Gets all registered CLR type full names.
    /// </summary>
    public IEnumerable<string> FullNames => _fullNameToTypeId.Keys;

    /// <summary>
    /// Gets the number of registered type mappings.
    /// </summary>
    public int Count => _fullNameToTypeId.Count;

    /// <summary>
    /// Infers the ATS type ID for a resource type when no explicit mapping exists.
    /// </summary>
    private static string InferResourceTypeId(Type resourceType)
    {
        var typeName = resourceType.Name;

        // Don't strip "Resource" suffix from interface types (IResource, IResourceWithXxx)
        var isInterface = typeName.StartsWith('I') && typeName.Length > 1 && char.IsUpper(typeName[1]);

        // Strip "Resource" suffix if present (but not for interfaces)
        if (!isInterface && typeName.EndsWith("Resource", StringComparison.Ordinal))
        {
            typeName = typeName[..^8];
        }

        return $"aspire/{typeName}";
    }

    /// <summary>
    /// Gets the ATS type ID for a type, or infers one if not explicitly mapped.
    /// </summary>
    /// <param name="type">The CLR type.</param>
    /// <returns>The ATS type ID (explicit or inferred).</returns>
    public string GetTypeIdOrInfer(Type type)
    {
        return GetTypeId(type) ?? InferResourceTypeId(type);
    }

    /// <summary>
    /// Merges this mapping with another, returning a new mapping containing both.
    /// </summary>
    /// <param name="other">The other mapping to merge.</param>
    /// <returns>A new mapping containing all entries from both mappings.</returns>
    /// <remarks>
    /// If both mappings contain the same CLR type, the entry from <paramref name="other"/> takes precedence.
    /// </remarks>
    public AtsTypeMapping Merge(AtsTypeMapping other)
    {
        var fullNameToTypeId = new Dictionary<string, string>(_fullNameToTypeId, StringComparer.Ordinal);
        var typeIdToFullName = new Dictionary<string, string>(_typeIdToFullName, StringComparer.Ordinal);

        foreach (var kvp in other._fullNameToTypeId)
        {
            fullNameToTypeId[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in other._typeIdToFullName)
        {
            typeIdToFullName[kvp.Key] = kvp.Value;
        }

        return new AtsTypeMapping(
            fullNameToTypeId.ToFrozenDictionary(StringComparer.Ordinal),
            typeIdToFullName.ToFrozenDictionary(StringComparer.Ordinal));
    }
}

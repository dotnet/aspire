// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;

namespace Aspire.Hosting.Ats;

/// <summary>
/// Attribute full name constants for scanning.
/// </summary>
internal static class AspireExportAttributeNames
{
    public const string FullName = "Aspire.Hosting.AspireExportAttribute";
    public const string IgnoreFullName = "Aspire.Hosting.AspireExportIgnoreAttribute";
}

/// <summary>
/// Provides CLR type to ATS type ID mapping based on [AspireExport] attribute declarations.
/// </summary>
/// <remarks>
/// <para>
/// Type IDs are automatically derived as <c>{AssemblyName}/{TypeName}</c>.
/// The mapping is used by:
/// <list type="bullet">
/// <item><description>Code generation (to generate correct TypeScript/Python types)</description></item>
/// <item><description>Runtime dispatcher (to validate handle types)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed partial class AtsTypeMapping
{
    private readonly FrozenDictionary<string, string> _fullNameToTypeId;
    private readonly FrozenDictionary<string, string> _typeIdToFullName;
    private readonly FrozenSet<string> _exposePropertiesTypeIds;

    /// <summary>
    /// Gets an empty type mapping with no registered types.
    /// </summary>
    public static AtsTypeMapping Empty { get; } = new AtsTypeMapping(
        FrozenDictionary<string, string>.Empty,
        FrozenDictionary<string, string>.Empty,
        FrozenSet<string>.Empty);

    private AtsTypeMapping(
        FrozenDictionary<string, string> fullNameToTypeId,
        FrozenDictionary<string, string> typeIdToFullName,
        FrozenSet<string> exposePropertiesTypeIds)
    {
        _fullNameToTypeId = fullNameToTypeId;
        _typeIdToFullName = typeIdToFullName;
        _exposePropertiesTypeIds = exposePropertiesTypeIds;
    }

    /// <summary>
    /// Creates a type mapping by scanning the specified assemblies using the interface abstraction.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan (as IAtsAssemblyInfo).</param>
    /// <returns>A type mapping containing all discovered type mappings.</returns>
    internal static AtsTypeMapping FromAssemblies(IEnumerable<IAtsAssemblyInfo> assemblies)
    {
        var fullNameToTypeId = new Dictionary<string, string>(StringComparer.Ordinal);
        var typeIdToFullName = new Dictionary<string, string>(StringComparer.Ordinal);
        var exposePropertiesTypeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var assembly in assemblies)
        {
            ScanAssembly(assembly, fullNameToTypeId, typeIdToFullName, exposePropertiesTypeIds);
        }

        return new AtsTypeMapping(
            fullNameToTypeId.ToFrozenDictionary(StringComparer.Ordinal),
            typeIdToFullName.ToFrozenDictionary(StringComparer.Ordinal),
            exposePropertiesTypeIds.ToFrozenSet(StringComparer.Ordinal));
    }

    /// <summary>
    /// Creates a type mapping by scanning a single assembly using the interface abstraction.
    /// </summary>
    /// <param name="assembly">The assembly to scan (as IAtsAssemblyInfo).</param>
    /// <returns>A type mapping containing all discovered type mappings.</returns>
    internal static AtsTypeMapping FromAssembly(IAtsAssemblyInfo assembly)
    {
        return FromAssemblies([assembly]);
    }

    /// <summary>
    /// Derives an ATS type ID from an assembly name and type name.
    /// </summary>
    /// <param name="assemblyName">The assembly name.</param>
    /// <param name="typeName">The type name (simple name, not full name).</param>
    /// <returns>The derived type ID in format {AssemblyName}/{TypeName}.</returns>
    public static string DeriveTypeId(string assemblyName, string typeName)
    {
        return $"{assemblyName}/{typeName}";
    }

    /// <summary>
    /// Derives an ATS type ID from a CLR type.
    /// </summary>
    /// <param name="type">The CLR type.</param>
    /// <returns>The derived type ID in format {AssemblyName}/{TypeName}.</returns>
    public static string DeriveTypeId(Type type)
    {
        var assemblyName = type.Assembly.GetName().Name ?? "Unknown";
        return DeriveTypeId(assemblyName, type.Name);
    }

    /// <summary>
    /// Derives an ATS type ID from a type's full name by extracting assembly and type name.
    /// </summary>
    /// <param name="typeFullName">The type's full name (e.g., "Aspire.Hosting.ContainerResource").</param>
    /// <returns>The derived type ID.</returns>
    public static string DeriveTypeIdFromFullName(string typeFullName)
    {
        // Extract the namespace as assembly name approximation and type name
        var lastDot = typeFullName.LastIndexOf('.');
        if (lastDot > 0)
        {
            var namespacePart = typeFullName[..lastDot];
            var typeName = typeFullName[(lastDot + 1)..];
            return $"{namespacePart}/{typeName}";
        }
        return typeFullName;
    }

    /// <summary>
    /// Scans an assembly using the interface abstraction.
    /// </summary>
    private static void ScanAssembly(
        IAtsAssemblyInfo assembly,
        Dictionary<string, string> fullNameToTypeId,
        Dictionary<string, string> typeIdToFullName,
        HashSet<string> exposePropertiesTypeIds)
    {
        var assemblyName = assembly.Name;

        // Scan assembly-level [AspireExport] attributes
        foreach (var attr in assembly.GetCustomAttributes())
        {
            if (attr.AttributeTypeFullName != AspireExportAttributeNames.FullName)
            {
                continue;
            }

            // Get Type - could be a type reference (decoded as string from metadata) or from named args
            string? targetTypeFullName = null;

            // First constructor argument might be the type (for assembly-level [AspireExport(typeof(X))])
            if (attr.FixedArguments.Count > 0 && attr.FixedArguments[0] is string typeArg)
            {
                targetTypeFullName = typeArg;
            }

            // Or check named argument "Type"
            if (targetTypeFullName == null &&
                attr.NamedArguments.TryGetValue("Type", out var typeObj) &&
                typeObj is string typeNamed)
            {
                targetTypeFullName = typeNamed;
            }

            if (string.IsNullOrEmpty(targetTypeFullName))
            {
                continue;
            }

            // Derive type ID from full name (uses namespace as assembly approximation)
            var typeId = DeriveTypeIdFromFullName(targetTypeFullName);
            fullNameToTypeId[targetTypeFullName] = typeId;
            typeIdToFullName[typeId] = targetTypeFullName;

            // Check for ExposeProperties
            if (attr.NamedArguments.TryGetValue("ExposeProperties", out var exposeObj) &&
                exposeObj is true)
            {
                exposePropertiesTypeIds.Add(typeId);
            }
        }

        // Scan type-level [AspireExport] attributes
        foreach (var type in assembly.GetTypes())
        {
            foreach (var attr in type.GetCustomAttributes())
            {
                if (attr.AttributeTypeFullName != AspireExportAttributeNames.FullName)
                {
                    continue;
                }

                // Type ID is derived from assembly name and type name
                var typeId = DeriveTypeId(assemblyName, type.Name);
                var fullName = type.FullName;

                if (!string.IsNullOrEmpty(fullName))
                {
                    fullNameToTypeId[fullName] = typeId;
                    typeIdToFullName[typeId] = fullName;
                }

                // Check for ExposeProperties
                if (attr.NamedArguments.TryGetValue("ExposeProperties", out var exposeObj) &&
                    exposeObj is true)
                {
                    exposePropertiesTypeIds.Add(typeId);
                }
            }
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
    /// Gets the ATS type ID for a type using the interface abstraction.
    /// </summary>
    /// <param name="type">The type (as IAtsTypeInfo).</param>
    /// <returns>The ATS type ID, or null if not mapped.</returns>
    internal string? GetTypeId(IAtsTypeInfo type)
    {
        return GetTypeId(type.FullName);
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
    /// Tries to get the ATS type ID for a type using the interface abstraction.
    /// </summary>
    /// <param name="type">The type (as IAtsTypeInfo).</param>
    /// <param name="typeId">The ATS type ID if found.</param>
    /// <returns>True if the type was found in the mapping.</returns>
    internal bool TryGetTypeId(IAtsTypeInfo type, out string? typeId)
    {
        return TryGetTypeId(type.FullName, out typeId);
    }

    /// <summary>
    /// Gets all registered type IDs from this mapping.
    /// </summary>
    public IEnumerable<string> RegisteredTypeIds => _typeIdToFullName.Keys;

    /// <summary>
    /// Gets all registered CLR type full names.
    /// </summary>
    public IEnumerable<string> FullNames => _fullNameToTypeId.Keys;

    /// <summary>
    /// Gets the number of registered type mappings.
    /// </summary>
    public int Count => _fullNameToTypeId.Count;

    /// <summary>
    /// Gets the ATS type ID for a type, deriving it from the assembly and type name if not explicitly mapped.
    /// </summary>
    /// <param name="type">The CLR type.</param>
    /// <returns>The ATS type ID (explicit or derived).</returns>
    public string GetTypeIdOrDerive(Type type)
    {
        return GetTypeId(type) ?? DeriveTypeId(type.Assembly.GetName().Name ?? "Unknown", type.Name);
    }

    /// <summary>
    /// Gets the ATS type ID for a type using the interface abstraction, deriving it if not explicitly mapped.
    /// </summary>
    /// <param name="type">The type (as IAtsTypeInfo).</param>
    /// <param name="assemblyName">The assembly name for derivation.</param>
    /// <returns>The ATS type ID (explicit or derived).</returns>
    internal string GetTypeIdOrDerive(IAtsTypeInfo type, string assemblyName)
    {
        return GetTypeId(type) ?? DeriveTypeId(assemblyName, type.Name);
    }

    /// <summary>
    /// Merges this mapping with another, returning a new mapping containing both.
    /// </summary>
    /// <param name="other">The other mapping to merge.</param>
    /// <returns>A new mapping containing all entries from both mappings.</returns>
    public AtsTypeMapping Merge(AtsTypeMapping other)
    {
        var fullNameToTypeId = new Dictionary<string, string>(_fullNameToTypeId, StringComparer.Ordinal);
        var typeIdToFullName = new Dictionary<string, string>(_typeIdToFullName, StringComparer.Ordinal);
        var exposePropertiesTypeIds = new HashSet<string>(_exposePropertiesTypeIds, StringComparer.Ordinal);

        foreach (var kvp in other._fullNameToTypeId)
        {
            fullNameToTypeId[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in other._typeIdToFullName)
        {
            typeIdToFullName[kvp.Key] = kvp.Value;
        }

        foreach (var typeId in other._exposePropertiesTypeIds)
        {
            exposePropertiesTypeIds.Add(typeId);
        }

        return new AtsTypeMapping(
            fullNameToTypeId.ToFrozenDictionary(StringComparer.Ordinal),
            typeIdToFullName.ToFrozenDictionary(StringComparer.Ordinal),
            exposePropertiesTypeIds.ToFrozenSet(StringComparer.Ordinal));
    }

    #region Type Classification

    /// <summary>
    /// Checks if a type ID represents a context type with exposed properties.
    /// </summary>
    /// <param name="typeId">The ATS type ID to check.</param>
    /// <returns>True if the type has ExposeProperties = true.</returns>
    public bool IsContextType(string? typeId)
    {
        return typeId != null && _exposePropertiesTypeIds.Contains(typeId);
    }

    /// <summary>
    /// Checks if a CLR type full name represents an IDistributedApplicationBuilder.
    /// </summary>
    public static bool IsBuilderType(string? fullName)
    {
        return fullName == "Aspire.Hosting.IDistributedApplicationBuilder";
    }

    /// <summary>
    /// Checks if a CLR type full name represents a DistributedApplication.
    /// </summary>
    public static bool IsApplicationType(string? fullName)
    {
        return fullName == "Aspire.Hosting.DistributedApplication";
    }

    /// <summary>
    /// Static helper to check if a type ID represents a wrapper type based on type ID format.
    /// For use when no type mapping instance is available.
    /// </summary>
    public static bool IsWrapperType(string? typeId)
    {
        if (string.IsNullOrEmpty(typeId) || !typeId.Contains('/'))
        {
            return false;
        }

        // Extract type name from {Assembly}/{TypeName}
        var slashIndex = typeId.LastIndexOf('/');
        var typeName = typeId[(slashIndex + 1)..];

        // Wrapper types are non-resource types like Configuration, Environment, Context, etc.
        return typeName switch
        {
            "IConfiguration" => true,
            "IHostEnvironment" => true,
            "ILogger" => true,
            "IServiceProvider" => true,
            "DistributedApplicationExecutionContext" => true,
            "EnvironmentCallbackContext" => true,
            "ResourceNotificationService" => true,
            "ResourceLoggerService" => true,
            "DistributedApplicationEventSubscription" => true,
            _ => false
        };
    }

    /// <summary>
    /// Static helper to check if a type ID represents a resource builder type based on type ID format.
    /// For use when no type mapping instance is available.
    /// </summary>
    public static bool IsResourceBuilderType(string? typeId)
    {
        if (string.IsNullOrEmpty(typeId) || !typeId.Contains('/'))
        {
            return false;
        }

        // Not a resource builder if it's a wrapper type
        if (IsWrapperType(typeId))
        {
            return false;
        }

        // Extract type name from {Assembly}/{TypeName}
        var slashIndex = typeId.LastIndexOf('/');
        var typeName = typeId[(slashIndex + 1)..];

        // IResource interfaces
        if (typeName.StartsWith("IResource", StringComparison.Ordinal))
        {
            return true;
        }

        // Concrete resource types (ends with Resource)
        if (typeName.EndsWith("Resource", StringComparison.Ordinal))
        {
            return true;
        }

        // Check for reference types (EndpointReference, etc.) - these are NOT resource builders
        if (typeName == "EndpointReference" || typeName == "ReferenceExpression")
        {
            return false;
        }

        return false;
    }

    /// <summary>
    /// Static helper to get the conventional parameter name for a type ID.
    /// For use when no type mapping instance is available.
    /// </summary>
    public static string GetParameterName(string? typeId)
    {
        if (string.IsNullOrEmpty(typeId) || !typeId.Contains('/'))
        {
            return "handle";
        }

        // Extract type name from {Assembly}/{TypeName}
        var slashIndex = typeId.LastIndexOf('/');
        var typeName = typeId[(slashIndex + 1)..];

        return typeName switch
        {
            "IDistributedApplicationBuilder" => "builder",
            "IConfiguration" => "configuration",
            "IHostEnvironment" => "environment",
            "DistributedApplicationExecutionContext" => "context",
            "EnvironmentCallbackContext" => "context",
            "IServiceProvider" => "serviceProvider",
            "ResourceNotificationService" => "notificationService",
            "ResourceLoggerService" => "loggerService",
            _ when IsResourceBuilderType(typeId) => "builder",
            _ => "handle"
        };
    }

    #endregion
}

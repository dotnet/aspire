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

    #region Type ID Constants

    /// <summary>
    /// Well-known ATS type ID constants.
    /// </summary>
    public static class TypeIds
    {
        // Core types
        /// <summary>The ATS type ID for IDistributedApplicationBuilder.</summary>
        public const string Builder = "aspire/Builder";
        /// <summary>The ATS type ID for DistributedApplication.</summary>
        public const string Application = "aspire/Application";
        /// <summary>The ATS type ID for DistributedApplicationExecutionContext.</summary>
        public const string ExecutionContext = "aspire/ExecutionContext";

        // Wrapper types (non-resource handles)
        /// <summary>The ATS type ID for IConfiguration.</summary>
        public const string Configuration = "aspire/Configuration";
        /// <summary>The ATS type ID for IHostEnvironment.</summary>
        public const string HostEnvironment = "aspire/HostEnvironment";
        /// <summary>The ATS type ID for EnvironmentCallbackContext.</summary>
        public const string EnvironmentContext = "aspire/EnvironmentContext";
        /// <summary>The ATS type ID for ILogger.</summary>
        public const string Logger = "aspire/Logger";
        /// <summary>The ATS type ID for DistributedApplicationEventSubscription.</summary>
        public const string EventSubscription = "aspire/EventSubscription";
        /// <summary>The ATS type ID for IServiceProvider.</summary>
        public const string ServiceProvider = "aspire/ServiceProvider";
        /// <summary>The ATS type ID for ResourceNotificationService.</summary>
        public const string ResourceNotificationService = "aspire/ResourceNotificationService";
        /// <summary>The ATS type ID for ResourceLoggerService.</summary>
        public const string ResourceLoggerService = "aspire/ResourceLoggerService";

        // Reference types
        /// <summary>The ATS type ID for EndpointReference.</summary>
        public const string EndpointReference = "aspire/EndpointReference";
        /// <summary>The ATS type ID for ReferenceExpression.</summary>
        public const string ReferenceExpression = "aspire/ReferenceExpression";

        // Resource interfaces
        /// <summary>The ATS type ID for IResource.</summary>
        public const string IResource = "aspire/IResource";
        /// <summary>The ATS type ID for IResourceWithEnvironment.</summary>
        public const string IResourceWithEnvironment = "aspire/IResourceWithEnvironment";
        /// <summary>The ATS type ID for IResourceWithEndpoints.</summary>
        public const string IResourceWithEndpoints = "aspire/IResourceWithEndpoints";
        /// <summary>The ATS type ID for IResourceWithArgs.</summary>
        public const string IResourceWithArgs = "aspire/IResourceWithArgs";
        /// <summary>The ATS type ID for IResourceWithConnectionString.</summary>
        public const string IResourceWithConnectionString = "aspire/IResourceWithConnectionString";
        /// <summary>The ATS type ID for IResourceWithWaitSupport.</summary>
        public const string IResourceWithWaitSupport = "aspire/IResourceWithWaitSupport";
        /// <summary>The ATS type ID for IResourceWithParent.</summary>
        public const string IResourceWithParent = "aspire/IResourceWithParent";

        // Concrete resources
        /// <summary>The ATS type ID for ContainerResource.</summary>
        public const string Container = "aspire/Container";
        /// <summary>The ATS type ID for ExecutableResource.</summary>
        public const string Executable = "aspire/Executable";
        /// <summary>The ATS type ID for ProjectResource.</summary>
        public const string Project = "aspire/Project";
        /// <summary>The ATS type ID for ParameterResource.</summary>
        public const string Parameter = "aspire/Parameter";
    }

    #endregion

    #region Type Classification

    /// <summary>
    /// Wrapper type IDs - non-resource types that get simple wrapper classes.
    /// </summary>
    private static readonly FrozenSet<string> s_wrapperTypeIds = FrozenSet.ToFrozenSet(
    [
        TypeIds.Configuration,
        TypeIds.HostEnvironment,
        TypeIds.ExecutionContext,
        TypeIds.Logger,
        TypeIds.EventSubscription,
        TypeIds.ServiceProvider,
        TypeIds.EnvironmentContext,
        TypeIds.ResourceNotificationService,
        TypeIds.ResourceLoggerService,
    ]);

    /// <summary>
    /// Parameter name conventions for wrapper types.
    /// </summary>
    private static readonly FrozenDictionary<string, string> s_parameterNames =
        new Dictionary<string, string>
        {
            [TypeIds.Builder] = "builder",
            [TypeIds.Configuration] = "configuration",
            [TypeIds.HostEnvironment] = "environment",
            [TypeIds.ExecutionContext] = "context",
            [TypeIds.EnvironmentContext] = "context",
            [TypeIds.ServiceProvider] = "serviceProvider",
            [TypeIds.ResourceNotificationService] = "notificationService",
            [TypeIds.ResourceLoggerService] = "loggerService",
        }.ToFrozenDictionary();

    /// <summary>
    /// Checks if a type ID represents a wrapper type (non-resource handle).
    /// Wrapper types get simple wrapper classes in generated code, not builder classes.
    /// </summary>
    /// <param name="typeId">The ATS type ID to check.</param>
    /// <returns>True if the type ID represents a wrapper type.</returns>
    public static bool IsWrapperType(string? typeId)
    {
        return typeId != null && s_wrapperTypeIds.Contains(typeId);
    }

    /// <summary>
    /// Checks if a type ID represents a resource builder type.
    /// Resource builder types get builder classes with fluent chaining in generated code.
    /// </summary>
    /// <param name="typeId">The ATS type ID to check.</param>
    /// <returns>True if the type ID represents a resource builder type.</returns>
    public static bool IsResourceBuilderType(string? typeId)
    {
        if (string.IsNullOrEmpty(typeId))
        {
            return false;
        }

        if (!typeId.StartsWith("aspire/", StringComparison.Ordinal))
        {
            return false;
        }

        if (IsWrapperType(typeId))
        {
            return false;
        }

        // Interface types (IResource, IResourceWithXxx)
        if (typeId.StartsWith("aspire/IResource", StringComparison.Ordinal))
        {
            return true;
        }

        // Well-known concrete resources
        if (typeId == TypeIds.Container ||
            typeId == TypeIds.Executable ||
            typeId == TypeIds.Project ||
            typeId == TypeIds.Parameter)
        {
            return true;
        }

        // Custom resources follow pattern: aspire/Xxx (no dots, not a wrapper)
        // Exclude reference types and special types
        if (typeId == TypeIds.EndpointReference ||
            typeId == TypeIds.ReferenceExpression ||
            typeId == TypeIds.Builder ||
            typeId == TypeIds.Application)
        {
            return false;
        }

        // Any other aspire/ type without dots is likely a resource type
        return !typeId.Contains('.');
    }

    /// <summary>
    /// Gets the conventional parameter name for a type ID.
    /// Used in generated code for method parameters.
    /// </summary>
    /// <param name="typeId">The ATS type ID.</param>
    /// <returns>The conventional parameter name.</returns>
    public static string GetParameterName(string? typeId)
    {
        if (typeId == null)
        {
            return "handle";
        }

        if (s_parameterNames.TryGetValue(typeId, out var name))
        {
            return name;
        }

        if (IsResourceBuilderType(typeId))
        {
            return "builder";
        }

        return "handle";
    }

    /// <summary>
    /// Infers the ATS type ID from a CLR type name.
    /// Used when no explicit mapping exists.
    /// </summary>
    /// <param name="typeName">The CLR type name (not full name).</param>
    /// <returns>The inferred ATS type ID.</returns>
    public static string InferTypeId(string typeName)
    {
        // Don't strip "Resource" suffix from interface types (IResource, IResourceWithXxx)
        var isInterface = typeName.StartsWith('I') && typeName.Length > 1 && char.IsUpper(typeName[1]);

        // Strip "Resource" suffix if present (but not for interfaces)
        if (!isInterface && typeName.EndsWith("Resource", StringComparison.Ordinal))
        {
            typeName = typeName[..^8];
        }

        return $"aspire/{typeName}";
    }

    #endregion
}

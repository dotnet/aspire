// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using Aspire.Hosting.CodeGeneration.Models.Types;

namespace Aspire.Hosting.CodeGeneration.Models.Ats;

/// <summary>
/// Provides CLR type to ATS type ID mapping for code generation.
/// This mirrors <c>Aspire.Hosting.Ats.AtsTypeMapping</c> but works with metadata reflection (RoType).
/// </summary>
/// <remarks>
/// <para>
/// Scans assemblies for <c>[AspireExport]</c> attributes with <c>AtsTypeId</c> set
/// on types or at assembly level.
/// </para>
/// <para>
/// Since RoType derives from System.Type, this uses Type.FullName as the lookup key,
/// making it compatible with both runtime and metadata reflection.
/// </para>
/// </remarks>
public sealed class AtsTypeMapping
{
    private const string AspireExportAttributeName = "Aspire.Hosting.AspireExportAttribute";

    private readonly FrozenDictionary<string, string> _fullNameToTypeId;

    /// <summary>
    /// Gets an empty type mapping with no registered types.
    /// </summary>
    public static AtsTypeMapping Empty { get; } = new AtsTypeMapping(
        FrozenDictionary<string, string>.Empty);

    private AtsTypeMapping(FrozenDictionary<string, string> fullNameToTypeId)
    {
        _fullNameToTypeId = fullNameToTypeId;
    }

    /// <summary>
    /// Creates a type mapping by scanning the specified assemblies for [AspireExport] declarations.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>A type mapping containing all discovered type mappings.</returns>
    public static AtsTypeMapping FromAssemblies(IEnumerable<RoAssembly> assemblies)
    {
        var fullNameToTypeId = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var assembly in assemblies)
        {
            ScanAssembly(assembly, fullNameToTypeId);
        }

        return new AtsTypeMapping(
            fullNameToTypeId.ToFrozenDictionary(StringComparer.Ordinal));
    }

    /// <summary>
    /// Creates a type mapping by scanning the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>A type mapping containing all discovered type mappings.</returns>
    public static AtsTypeMapping FromAssembly(RoAssembly assembly)
    {
        return FromAssemblies([assembly]);
    }

    private static void ScanAssembly(RoAssembly assembly, Dictionary<string, string> fullNameToTypeId)
    {
        // Scan assembly-level [AspireExport] attributes
        foreach (var attr in assembly.GetCustomAttributes())
        {
            if (attr.AttributeType.FullName != AspireExportAttributeName)
            {
                continue;
            }

            // Get AtsTypeId from named arguments
            var atsTypeId = GetNamedArgument<string>(attr, "AtsTypeId");
            if (string.IsNullOrEmpty(atsTypeId))
            {
                continue;
            }

            // Get Type from constructor argument or named argument
            var targetType = GetConstructorArgument<RoType>(attr, 0)
                ?? GetNamedArgument<RoType>(attr, "Type");

            if (targetType?.FullName != null)
            {
                fullNameToTypeId[targetType.FullName] = atsTypeId;
            }
        }

        // Scan type-level [AspireExport] attributes
        foreach (var type in assembly.GetTypeDefinitions())
        {
            foreach (var attr in type.GetCustomAttributes())
            {
                if (attr.AttributeType.FullName != AspireExportAttributeName)
                {
                    continue;
                }

                // Get AtsTypeId from named arguments
                var atsTypeId = GetNamedArgument<string>(attr, "AtsTypeId");
                if (string.IsNullOrEmpty(atsTypeId))
                {
                    continue;
                }

                if (type.FullName != null)
                {
                    fullNameToTypeId[type.FullName] = atsTypeId;
                }
            }
        }
    }

    private static T? GetNamedArgument<T>(RoCustomAttributeData attr, string name) where T : class
    {
        var kvp = attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == name);
        return kvp.Key != null ? kvp.Value as T : null;
    }

    private static T? GetConstructorArgument<T>(RoCustomAttributeData attr, int index) where T : class
    {
        return attr.FixedArguments.Count > index ? attr.FixedArguments[index] as T : null;
    }

    /// <summary>
    /// Gets the ATS type ID for a CLR type.
    /// </summary>
    /// <param name="type">The CLR type.</param>
    /// <returns>The ATS type ID, or null if not mapped.</returns>
    public string? GetTypeId(RoType type)
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
    /// Tries to get the ATS type ID for a CLR type.
    /// </summary>
    /// <param name="type">The CLR type.</param>
    /// <param name="typeId">The ATS type ID if found.</param>
    /// <returns>True if the type was found in the mapping.</returns>
    public bool TryGetTypeId(RoType type, out string? typeId)
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
    /// Gets the number of registered type mappings.
    /// </summary>
    public int Count => _fullNameToTypeId.Count;

    /// <summary>
    /// Gets all registered CLR type full names.
    /// </summary>
    public IEnumerable<string> FullNames => _fullNameToTypeId.Keys;

    /// <summary>
    /// Gets all registered ATS type IDs.
    /// </summary>
    public IEnumerable<string> RegisteredTypeIds => _fullNameToTypeId.Values.Distinct();

    /// <summary>
    /// Infers the ATS type ID for a resource type when no explicit mapping exists.
    /// </summary>
    private static string InferResourceTypeId(RoType resourceType)
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
    public string GetTypeIdOrInfer(RoType type)
    {
        return GetTypeId(type) ?? InferResourceTypeId(type);
    }

    /// <summary>
    /// Merges this mapping with another, returning a new mapping containing both.
    /// </summary>
    /// <param name="other">The other mapping to merge.</param>
    /// <returns>A new mapping containing all entries from both mappings.</returns>
    public AtsTypeMapping Merge(AtsTypeMapping other)
    {
        var fullNameToTypeId = new Dictionary<string, string>(_fullNameToTypeId, StringComparer.Ordinal);

        foreach (var kvp in other._fullNameToTypeId)
        {
            fullNameToTypeId[kvp.Key] = kvp.Value;
        }

        return new AtsTypeMapping(
            fullNameToTypeId.ToFrozenDictionary(StringComparer.Ordinal));
    }

    #region Type ID Constants

    /// <summary>
    /// Well-known ATS type ID constants.
    /// Mirrors Aspire.Hosting.Ats.AtsTypeMapping.TypeIds for use in code generation.
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

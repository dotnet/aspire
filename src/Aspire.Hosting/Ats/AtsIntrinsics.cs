// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

/// <summary>
/// Defines the intrinsic Aspire types that ATS recognizes natively.
/// IResourceBuilder&lt;T&gt; is first-class - type ID and hierarchy derived from T.
/// </summary>
public static class AtsIntrinsics
{
    /// <summary>
    /// Maps .NET types to their ATS type IDs.
    /// </summary>
    private static readonly Dictionary<Type, string> s_intrinsicTypes = new()
    {
        // Application lifecycle
        [typeof(IDistributedApplicationBuilder)] = "aspire/Builder",
        [typeof(DistributedApplication)] = "aspire/Application",

        // Execution context
        [typeof(DistributedApplicationExecutionContext)] = "aspire/ExecutionContext",

        // References
        [typeof(EndpointReference)] = "aspire/EndpointReference",
        [typeof(ReferenceExpression)] = "aspire/ReferenceExpression",

        // Callback contexts
        [typeof(EnvironmentCallbackContext)] = "aspire/EnvironmentContext",

        // Builder properties
        [typeof(IConfiguration)] = "aspire/Configuration",
        [typeof(IHostEnvironment)] = "aspire/HostEnvironment",

        // Eventing
        [typeof(DistributedApplicationEventSubscription)] = "aspire/EventSubscription",

        // Services
        [typeof(IServiceProvider)] = "aspire/ServiceProvider",
        [typeof(ResourceNotificationService)] = "aspire/ResourceNotificationService",
        [typeof(ResourceLoggerService)] = "aspire/ResourceLoggerService",

        // Logging
        [typeof(Microsoft.Extensions.Logging.ILogger)] = "aspire/Logger",

        // Resource interface types (for AppliesTo constraints)
        [typeof(IResource)] = "aspire/IResource",
        [typeof(IResourceWithEnvironment)] = "aspire/IResourceWithEnvironment",
        [typeof(IResourceWithEndpoints)] = "aspire/IResourceWithEndpoints",
        [typeof(IResourceWithArgs)] = "aspire/IResourceWithArgs",
        [typeof(IResourceWithConnectionString)] = "aspire/IResourceWithConnectionString",
        [typeof(IResourceWithWaitSupport)] = "aspire/IResourceWithWaitSupport",
        [typeof(IResourceWithParent)] = "aspire/IResourceWithParent",
    };

    /// <summary>
    /// Cache for resource type ID lookups.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, string> s_resourceTypeCache = new();

    /// <summary>
    /// Gets the ATS type ID for a .NET type, or null if not intrinsic.
    /// </summary>
    public static string? GetTypeId(Type type)
    {
        // Direct match in intrinsic types
        if (s_intrinsicTypes.TryGetValue(type, out var typeId))
        {
            return typeId;
        }

        // IResourceBuilder<T> - derive from resource type T
        var resourceType = GetResourceType(type);
        if (resourceType != null)
        {
            return GetResourceTypeId(resourceType);
        }

        // Any IResource type - derive type ID from the type name
        if (typeof(IResource).IsAssignableFrom(type))
        {
            return GetResourceTypeId(type);
        }

        // Check interfaces for intrinsic types
        foreach (var iface in type.GetInterfaces())
        {
            if (s_intrinsicTypes.TryGetValue(iface, out typeId))
            {
                return typeId;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the resource type T from IResourceBuilder&lt;T&gt;, or null if not a resource builder.
    /// </summary>
    public static Type? GetResourceType(Type type)
    {
        // Direct IResourceBuilder<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IResourceBuilder<>))
        {
            return type.GetGenericArguments()[0];
        }

        // Check interfaces for IResourceBuilder<T>
        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IResourceBuilder<>))
            {
                return iface.GetGenericArguments()[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the ATS type ID for a resource type.
    /// Derived from CLR type name: RedisResource → "aspire/Redis"
    /// </summary>
    public static string GetResourceTypeId(Type resourceType)
    {
        return s_resourceTypeCache.GetOrAdd(resourceType, static type =>
        {
            // Strip "Resource" suffix if present
            var name = type.Name;
            if (name.EndsWith("Resource", StringComparison.Ordinal))
            {
                name = name[..^8];
            }
            return $"aspire/{name}";
        });
    }

    /// <summary>
    /// Checks if a resource type is assignable to a target resource type.
    /// Uses CLR type inheritance directly.
    /// </summary>
    public static bool IsResourceAssignableTo(Type resourceType, Type targetResourceType)
    {
        return targetResourceType.IsAssignableFrom(resourceType);
    }

    /// <summary>
    /// Checks if a type is an intrinsic Aspire type.
    /// </summary>
    public static bool IsIntrinsic(Type type)
    {
        return GetTypeId(type) != null;
    }

    /// <summary>
    /// Checks if a type is an IResourceBuilder&lt;T&gt;.
    /// </summary>
    public static bool IsResourceBuilder(Type type)
    {
        return GetResourceType(type) != null;
    }

    /// <summary>
    /// Simple types that can be serialized directly as JSON primitives.
    /// </summary>
    private static readonly HashSet<Type> s_simpleTypes =
    [
        typeof(string),
        typeof(bool),
        typeof(int),
        typeof(long),
        typeof(double),
        typeof(float),
        typeof(decimal),
        typeof(byte),
        typeof(short),
        typeof(uint),
        typeof(ulong),
        typeof(ushort),
        typeof(sbyte),
        typeof(char),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(Uri)
    ];

    /// <summary>
    /// Checks if a type is ATS-compatible (can be marshalled across the boundary).
    /// Compatible types: simple types, enums, intrinsic types, nullable versions of these.
    /// </summary>
    public static bool IsAtsCompatible(Type type)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        // Simple/primitive types
        if (s_simpleTypes.Contains(underlyingType))
        {
            return true;
        }

        // Enums
        if (underlyingType.IsEnum)
        {
            return true;
        }

        // Intrinsic Aspire types (handles)
        if (IsIntrinsic(underlyingType))
        {
            return true;
        }

        return false;
    }
}

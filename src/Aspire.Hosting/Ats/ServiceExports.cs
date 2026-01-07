// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Ats;

/// <summary>
/// ATS exports for service provider access.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IServiceProvider"/> is available after the application is built.
/// Services can be resolved using their ATS type IDs for type-safe access.
/// </para>
/// <para>
/// <strong>Available Services:</strong>
/// <list type="bullet">
///   <item><description><c>Aspire.Hosting.ApplicationModel/ResourceNotificationService</c> - Resource state notifications</description></item>
///   <item><description><c>Aspire.Hosting.ApplicationModel/ResourceLoggerService</c> - Resource-specific logging</description></item>
/// </list>
/// </para>
/// </remarks>
internal static class ServiceExports
{
    /// <summary>
    /// Maps CLR type full names to .NET types for service resolution.
    /// </summary>
    private static readonly Dictionary<string, Type> s_serviceTypes = new()
    {
        ["Aspire.Hosting.ApplicationModel.ResourceNotificationService"] = typeof(ResourceNotificationService),
        ["Aspire.Hosting.ApplicationModel.ResourceLoggerService"] = typeof(ResourceLoggerService),
    };

    /// <summary>
    /// Gets the service provider from the builder's execution context.
    /// </summary>
    /// <remarks>
    /// The service provider is only available after <c>Aspire.Hosting/build</c> has been called.
    /// Before build, accessing the service provider will throw an exception.
    /// </remarks>
    /// <param name="builder">The builder handle.</param>
    /// <returns>A handle to the <see cref="IServiceProvider"/>.</returns>
    [AspireExport("getServiceProvider", Description = "Gets the service provider from the builder")]
    public static IServiceProvider GetServiceProvider(IDistributedApplicationBuilder builder)
    {
        return builder.ExecutionContext.ServiceProvider;
    }

    /// <summary>
    /// Gets the ResourceNotificationService from the service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider handle.</param>
    /// <returns>A handle to the <see cref="ResourceNotificationService"/>.</returns>
    [AspireExport("getResourceNotificationService", Description = "Gets the resource notification service")]
    public static ResourceNotificationService GetResourceNotificationService(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ResourceNotificationService>();
    }

    /// <summary>
    /// Gets the ResourceLoggerService from the service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider handle.</param>
    /// <returns>A handle to the <see cref="ResourceLoggerService"/>.</returns>
    [AspireExport("getResourceLoggerService", Description = "Gets the resource logger service")]
    public static ResourceLoggerService GetResourceLoggerService(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ResourceLoggerService>();
    }

    /// <summary>
    /// Gets a service by its ATS type ID.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provides a generic way to resolve services using ATS type IDs instead of
    /// CLR type names. Only services registered in the ATS type system can be resolved.
    /// </para>
    /// <para>
    /// <strong>Example:</strong>
    /// <code>
    /// // Get ResourceNotificationService by ATS type ID
    /// const notifications = await client.invoke("Aspire.Hosting/getService", {
    ///     serviceProvider,
    ///     typeId: "Aspire.Hosting.ApplicationModel/ResourceNotificationService"
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    /// <param name="serviceProvider">The service provider handle.</param>
    /// <param name="typeId">The ATS type ID (e.g., "Aspire.Hosting.ApplicationModel/ResourceNotificationService").</param>
    /// <returns>A handle to the service, or null if not found.</returns>
    [AspireExport("getService", Description = "Gets a service by ATS type ID")]
    public static object? GetService(IServiceProvider serviceProvider, string typeId)
    {
        if (!s_serviceTypes.TryGetValue(typeId, out var type))
        {
            throw new InvalidOperationException(
                $"Service type '{typeId}' is not available through ATS. " +
                $"Available types: {string.Join(", ", s_serviceTypes.Keys)}");
        }

        return serviceProvider.GetService(type);
    }

    /// <summary>
    /// Gets a required service by its ATS type ID.
    /// </summary>
    /// <remarks>
    /// Like <c>Aspire.Hosting/getService</c>, but throws if the service is not registered.
    /// </remarks>
    /// <param name="serviceProvider">The service provider handle.</param>
    /// <param name="typeId">The ATS type ID.</param>
    /// <returns>A handle to the service.</returns>
    [AspireExport("getRequiredService", Description = "Gets a required service by ATS type ID")]
    public static object GetRequiredService(IServiceProvider serviceProvider, string typeId)
    {
        if (!s_serviceTypes.TryGetValue(typeId, out var type))
        {
            throw new InvalidOperationException(
                $"Service type '{typeId}' is not available through ATS. " +
                $"Available types: {string.Join(", ", s_serviceTypes.Keys)}");
        }

        return serviceProvider.GetRequiredService(type);
    }
}

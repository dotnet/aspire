// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Ats;

/// <summary>
/// ATS exports for <see cref="IDistributedApplicationBuilder"/> properties and related types.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IDistributedApplicationBuilder"/> is the central type for defining Aspire applications.
/// This class exposes its properties (Configuration, Environment, AppHostDirectory) and provides
/// capabilities to interact with each of them.
/// </para>
/// <para>
/// <strong>Builder Properties:</strong>
/// <list type="bullet">
///   <item><description><c>Configuration</c> - Application configuration (connection strings, settings)</description></item>
///   <item><description><c>Environment</c> - Host environment info (name, isDevelopment)</description></item>
///   <item><description><c>AppHostDirectory</c> - Directory containing the app host</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Lifecycle Events:</strong>
/// <list type="bullet">
///   <item><description><c>subscribeBeforeStart</c> - Called before the application starts</description></item>
///   <item><description><c>subscribeAfterResourcesCreated</c> - Called after resources are created</description></item>
/// </list>
/// </para>
/// </remarks>
internal static class BuilderExports
{
    #region Builder Properties

    /// <summary>
    /// Gets the configuration from the builder.
    /// </summary>
    /// <param name="builder">The builder handle.</param>
    /// <returns>A handle to the <see cref="IConfiguration"/>.</returns>
    [AspireExport("aspire/getConfiguration@1", Description = "Gets the configuration from the builder")]
    public static IConfiguration GetConfiguration(IDistributedApplicationBuilder builder)
    {
        return builder.Configuration;
    }

    /// <summary>
    /// Gets the host environment from the builder.
    /// </summary>
    /// <param name="builder">The builder handle.</param>
    /// <returns>A handle to the <see cref="IHostEnvironment"/>.</returns>
    [AspireExport("aspire/getEnvironment@1", Description = "Gets the host environment from the builder")]
    public static IHostEnvironment GetEnvironment(IDistributedApplicationBuilder builder)
    {
        return builder.Environment;
    }

    /// <summary>
    /// Gets the app host directory.
    /// </summary>
    /// <param name="builder">The builder handle.</param>
    /// <returns>The directory path containing the app host.</returns>
    [AspireExport("aspire/getAppHostDirectory@1", Description = "Gets the app host directory")]
    public static string GetAppHostDirectory(IDistributedApplicationBuilder builder)
    {
        return builder.AppHostDirectory;
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    /// <param name="configuration">The configuration handle.</param>
    /// <param name="key">The configuration key (e.g., "ConnectionStrings:Default").</param>
    /// <returns>The configuration value, or null if not found.</returns>
    [AspireExport("aspire/getConfigValue@1", Description = "Gets a configuration value by key")]
    public static string? GetConfigValue(IConfiguration configuration, string key)
    {
        return configuration[key];
    }

    /// <summary>
    /// Gets a connection string by name.
    /// </summary>
    /// <param name="configuration">The configuration handle.</param>
    /// <param name="name">The connection string name.</param>
    /// <returns>The connection string value, or null if not found.</returns>
    [AspireExport("aspire/getConnectionString@1", Description = "Gets a connection string by name")]
    public static string? GetConnectionString(IConfiguration configuration, string name)
    {
        return configuration.GetConnectionString(name);
    }

    #endregion

    #region Host Environment

    /// <summary>
    /// Gets the environment name (e.g., "Development", "Production").
    /// </summary>
    /// <param name="environment">The host environment handle.</param>
    /// <returns>The environment name.</returns>
    [AspireExport("aspire/getEnvironmentName@1", Description = "Gets the environment name")]
    public static string GetEnvironmentName(IHostEnvironment environment)
    {
        return environment.EnvironmentName;
    }

    /// <summary>
    /// Checks if the environment is Development.
    /// </summary>
    /// <param name="environment">The host environment handle.</param>
    /// <returns>True if running in Development environment.</returns>
    [AspireExport("aspire/isDevelopment@1", Description = "Checks if running in Development environment")]
    public static bool IsDevelopment(IHostEnvironment environment)
    {
        return environment.IsDevelopment();
    }

    #endregion

    #region Lifecycle Events

    /// <summary>
    /// Subscribes to the BeforeStart event, which fires before the application starts.
    /// </summary>
    /// <remarks>
    /// This event provides access to the service provider and distributed application model,
    /// allowing you to perform final configuration or validation before resources start.
    /// </remarks>
    /// <param name="builder">The builder handle.</param>
    /// <param name="callback">A callback that receives the service provider when the event fires.</param>
    /// <returns>A subscription handle that can be used to unsubscribe.</returns>
    [AspireExport("aspire/subscribeBeforeStart@1", Description = "Subscribes to the BeforeStart lifecycle event")]
    public static DistributedApplicationEventSubscription SubscribeBeforeStart(
        IDistributedApplicationBuilder builder,
        [AspireCallback("aspire/BeforeStartCallback")] Func<IServiceProvider, Task> callback)
    {
        return builder.Eventing.Subscribe<BeforeStartEvent>(async (@event, ct) =>
        {
            await callback(@event.Services).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Subscribes to the AfterResourcesCreated event, which fires after all resources are created.
    /// </summary>
    /// <remarks>
    /// At this point, all resources have been instantiated but may not yet be running.
    /// This is useful for performing cross-resource configuration.
    /// </remarks>
    /// <param name="builder">The builder handle.</param>
    /// <param name="callback">A callback that receives the service provider when the event fires.</param>
    /// <returns>A subscription handle that can be used to unsubscribe.</returns>
    [AspireExport("aspire/subscribeAfterResourcesCreated@1", Description = "Subscribes to the AfterResourcesCreated lifecycle event")]
    public static DistributedApplicationEventSubscription SubscribeAfterResourcesCreated(
        IDistributedApplicationBuilder builder,
        [AspireCallback("aspire/AfterResourcesCreatedCallback")] Func<IServiceProvider, Task> callback)
    {
        return builder.Eventing.Subscribe<AfterResourcesCreatedEvent>(async (@event, ct) =>
        {
            await callback(@event.Services).ConfigureAwait(false);
        });
    }

    #endregion
}

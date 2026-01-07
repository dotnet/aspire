// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Ats;

/// <summary>
/// Core ATS (Aspire Type System) exports for polyglot app host support.
/// </summary>
/// <remarks>
/// <para>
/// This class defines the foundational capabilities that enable non-.NET languages (TypeScript, Python, etc.)
/// to build Aspire distributed applications. These exports form the stable API surface for polyglot app hosts.
/// </para>
/// <para>
/// <strong>Design Principles:</strong>
/// <list type="bullet">
///   <item><description>Capabilities are the contract - not CLR method signatures</description></item>
///   <item><description>Handles replace direct object references - guest code never sees .NET types</description></item>
///   <item><description>Capability IDs use format {Package}/{Method}</description></item>
///   <item><description>.NET implementation details are hidden behind a stable polyglot surface</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Capability Naming Convention:</strong> <c>{Package}/{operation}</c>
/// </para>
/// <para>
/// <strong>Usage from TypeScript:</strong>
/// <code>
/// // Create builder and add resources
/// const builder = await client.invoke("Aspire.Hosting/createBuilder", {});
/// const redis = await client.invoke("Aspire.Hosting/addContainer", { builder, name: "cache", image: "redis:latest" });
/// await client.invoke("Aspire.Hosting/withEnvironment", { resource: redis, name: "REDIS_MODE", value: "standalone" });
///
/// // Build and run
/// const app = await client.invoke("Aspire.Hosting/build", { builder });
/// await client.invoke("Aspire.Hosting/run", { app });
/// </code>
/// </para>
/// </remarks>
internal static class CoreExports
{
    #region Application Lifecycle

    /// <summary>
    /// Creates a new distributed application builder.
    /// </summary>
    /// <remarks>
    /// This is the entry point for polyglot app hosts. The returned builder handle is used
    /// to add resources, configure the application, and eventually build and run it.
    /// </remarks>
    /// <param name="args">Optional command-line arguments to pass to the builder.</param>
    /// <returns>A handle to the <see cref="IDistributedApplicationBuilder"/>.</returns>
    [AspireExport("createBuilder", Description = "Creates a new distributed application builder")]
    public static IDistributedApplicationBuilder CreateBuilder(string[]? args = null)
    {
        return DistributedApplication.CreateBuilder(args ?? []);
    }

    /// <summary>
    /// Builds the distributed application from the configured builder.
    /// </summary>
    /// <remarks>
    /// Call this after all resources have been added and configured. The returned application
    /// handle can then be passed to <c>Aspire.Hosting/run</c> to start orchestration.
    /// </remarks>
    /// <param name="builder">The builder handle from <c>Aspire.Hosting/createBuilder</c>.</param>
    /// <returns>A handle to the built <see cref="DistributedApplication"/>.</returns>
    [AspireExport("build", Description = "Builds the distributed application")]
    public static DistributedApplication Build(IDistributedApplicationBuilder builder)
    {
        return builder.Build();
    }

    /// <summary>
    /// Runs the distributed application, starting all configured resources.
    /// </summary>
    /// <remarks>
    /// This starts the Aspire orchestrator which will launch containers, executables,
    /// and other resources. The method completes when the application shuts down.
    /// </remarks>
    /// <param name="app">The application handle from <c>Aspire.Hosting/build</c>.</param>
    /// <returns>A task that completes when the application stops.</returns>
    [AspireExport("run", Description = "Runs the distributed application")]
    public static Task Run(DistributedApplication app)
    {
        return app.RunAsync();
    }

    /// <summary>
    /// Gets the execution context from the builder.
    /// </summary>
    /// <remarks>
    /// The execution context provides information about the current operation mode
    /// (run vs publish) and other contextual information.
    /// </remarks>
    /// <param name="builder">The builder handle.</param>
    /// <returns>A handle to the <see cref="DistributedApplicationExecutionContext"/>.</returns>
    [AspireExport("getExecutionContext", Description = "Gets the execution context from the builder")]
    public static DistributedApplicationExecutionContext GetExecutionContext(IDistributedApplicationBuilder builder)
    {
        return builder.ExecutionContext;
    }

    #endregion

    #region Execution Context

    /// <summary>
    /// Checks if the application is running in run mode.
    /// </summary>
    /// <remarks>
    /// Run mode is the default mode when developing locally. Resources are started
    /// and orchestrated by the Aspire host.
    /// </remarks>
    /// <param name="context">The execution context handle.</param>
    /// <returns>True if in run mode.</returns>
    [AspireExport("isRunMode", Description = "Checks if running in run mode")]
    public static bool IsRunMode(DistributedApplicationExecutionContext context)
    {
        return context.IsRunMode;
    }

    /// <summary>
    /// Checks if the application is running in publish mode.
    /// </summary>
    /// <remarks>
    /// Publish mode is used when generating deployment artifacts (e.g., for Azure, Kubernetes).
    /// Resources are not started; instead, manifests are generated.
    /// </remarks>
    /// <param name="context">The execution context handle.</param>
    /// <returns>True if in publish mode.</returns>
    [AspireExport("isPublishMode", Description = "Checks if running in publish mode")]
    public static bool IsPublishMode(DistributedApplicationExecutionContext context)
    {
        return context.IsPublishMode;
    }

    #endregion

    #region Environment Configuration

    /// <summary>
    /// Adds an environment variable callback to a resource.
    /// </summary>
    /// <remarks>
    /// The callback is invoked during resource startup, allowing dynamic environment
    /// variable configuration based on runtime state. The callback receives a context
    /// with access to environment variables and resource information.
    /// </remarks>
    /// <param name="resource">The resource builder handle.</param>
    /// <param name="callback">A callback ID registered with the guest runtime.</param>
    /// <returns>The same resource builder handle for chaining.</returns>
    [AspireExport("withEnvironmentCallback", Description = "Adds an environment callback")]
    public static IResourceBuilder<IResourceWithEnvironment> WithEnvironmentCallback(
        IResourceBuilder<IResourceWithEnvironment> resource,
        Func<EnvironmentCallbackContext, Task> callback)
    {
        return resource.WithEnvironment(callback);
    }

    #endregion

    #region Endpoint Configuration

    /// <summary>
    /// Gets an endpoint reference from a resource.
    /// </summary>
    /// <remarks>
    /// Endpoint references can be used to create connection strings or URLs
    /// that reference the endpoint at runtime.
    /// </remarks>
    /// <param name="resource">The resource builder handle.</param>
    /// <param name="name">The endpoint name (e.g., "http", "tcp").</param>
    /// <returns>A handle to the endpoint reference.</returns>
    [AspireExport("getEndpoint", Description = "Gets an endpoint reference")]
    public static EndpointReference GetEndpoint(
        IResourceBuilder<IResourceWithEndpoints> resource,
        string name)
    {
        return resource.GetEndpoint(name);
    }

    #endregion

    #region Resource Dependencies

    /// <summary>
    /// Adds a reference from one resource to another.
    /// </summary>
    /// <remarks>
    /// References inject connection information (connection strings, URLs) from the
    /// dependency into the resource's environment. This enables service discovery.
    /// </remarks>
    /// <param name="resource">The resource builder handle.</param>
    /// <param name="dependency">The dependency resource handle.</param>
    /// <returns>The same resource builder handle for chaining.</returns>
    [AspireExport("withReference", Description = "Adds a reference to another resource")]
    public static IResourceBuilder<IResourceWithEnvironment> WithReference(
        IResourceBuilder<IResourceWithEnvironment> resource,
        IResourceBuilder<IResourceWithConnectionString> dependency)
    {
        return resource.WithReference(dependency);
    }

    #endregion

    #region Container Configuration

    /// <summary>
    /// Adds a volume to a container resource.
    /// </summary>
    /// <remarks>
    /// Volumes persist data across container restarts. Named volumes are managed
    /// by Docker/Podman and stored in a system-managed location.
    /// </remarks>
    /// <param name="resource">The container resource builder handle.</param>
    /// <param name="target">The mount path inside the container.</param>
    /// <param name="name">The volume name. If null, an anonymous volume is created.</param>
    /// <param name="isReadOnly">Whether the volume is read-only.</param>
    /// <returns>The same resource builder handle for chaining.</returns>
    [AspireExport("withVolume", Description = "Adds a volume")]
    public static IResourceBuilder<ContainerResource> WithVolume(
        IResourceBuilder<ContainerResource> resource,
        string target,
        string? name = null,
        bool isReadOnly = false)
    {
        return resource.WithVolume(name, target, isReadOnly);
    }

    #endregion

    #region Resource Information

    /// <summary>
    /// Gets the name of the resource from a builder.
    /// </summary>
    /// <param name="resource">The resource builder handle.</param>
    /// <returns>The resource name.</returns>
    [AspireExport("getResourceName", Description = "Gets the resource name")]
    public static string GetResourceName(IResourceBuilder<IResource> resource)
    {
        return resource.Resource.Name;
    }

    #endregion

    #region Parameters

    /// <summary>
    /// Sets a description for a parameter resource.
    /// </summary>
    /// <remarks>
    /// Descriptions help users understand what value should be provided for the parameter.
    /// </remarks>
    /// <param name="resource">The parameter resource builder handle.</param>
    /// <param name="description">The description text.</param>
    /// <returns>The same resource builder handle for chaining.</returns>
    [AspireExport("withDescription", Description = "Sets a parameter description")]
    public static IResourceBuilder<ParameterResource> WithDescription(
        IResourceBuilder<ParameterResource> resource,
        string description)
    {
        return resource.WithDescription(description);
    }

    #endregion
}

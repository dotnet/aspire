// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for creating Aspire Dashboard resources in the application model.
/// </summary>
public static class DockerComposeAspireDashboardResourceBuilderExtensions
{
    /// <summary>
    /// Creates a new Aspire Dashboard resource builder with the specified name.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> instance.</param>
    /// <param name="name">The name of the Aspire Dashboard resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AspireDashboardResource}"/>.</returns>
    /// <remarks>
    /// This method initializes a new Aspire Dashboard resource that can be used to visualize telemetry data
    /// in the Aspire Hosting environment. The resource is not automatically added to the application model;
    /// instead, it returns a builder that can be further configured and added as needed.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <c>null</c> or empty.</exception>
    internal static IResourceBuilder<DockerComposeAspireDashboardResource> CreateDashboard(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var resource = new DockerComposeAspireDashboardResource(name);

        // Initialize the dashboard resource
        return builder.CreateResourceBuilder(resource)
                      .WithImage("mcr.microsoft.com/dotnet/nightly/aspire-dashboard")
                      .WithHttpEndpoint(targetPort: 18888)
                      .WithHttpEndpoint(name: "otlp-grpc", targetPort: 18889);
    }

    /// <summary>
    /// Configures the host port that the Aspire Dashboard resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{DockerComposeAspireDashboardResource}"/> instance to configure.</param>
    /// <param name="port">The port to bind on the host. If <c>null</c> a random port will be assigned.</param>
    /// <returns>
    /// The <see cref="IResourceBuilder{DockerComposeAspireDashboardResource}"/> instance for chaining.
    /// </returns>
    public static IResourceBuilder<DockerComposeAspireDashboardResource> WithHostPort(
        this IResourceBuilder<DockerComposeAspireDashboardResource> builder,
        int? port = null)
    {
        return builder.WithEndpoint("http", e =>
        {
            e.Port = port;
            e.IsExternal = port is not null;
        });
    }
}

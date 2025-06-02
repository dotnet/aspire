// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for creating Aspire Dashboard resources in the application model.
/// </summary>
public static class AspireDashboardResourceBuilderExtensions
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
    /// <exception cref="ArgumentNullException">Thrown when the builder is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>  
    public static IResourceBuilder<AspireDashboardResource> CreateDashboard(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var resource = new AspireDashboardResource(name);

        // Initialize the dashboard resource
        return builder.CreateResourceBuilder(resource)
                      .WithImage("mcr.microsoft.com/dotnet/nightly/aspire-dashboard")
                      .WithHttpEndpoint(targetPort: 18888)
                      // Expose the browser endpoint by default, there's auth required to access it
                      .WithEndpoint("http", e => e.IsExternal = true)
                      .WithHttpEndpoint(name: "otlp-grpc", targetPort: 18889);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Dev tunnels resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class DevTunnelsResourceBuilderExtensions
{
    /// <summary>
    /// Adds a Dev tunnels resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. </param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// Dev tunnels provide secure access to your local services from anywhere.
    /// <example>
    /// Use in application host
    /// <code lang="csharp">
    /// var devTunnel = builder.AddDevTunnel("mytunnel");
    ///
    /// var myService = builder.AddProject&lt;Projects.MyService&lt;()
    ///                        .WithDevTunnel(devTunnel);
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<DevTunnelResource> AddDevTunnel(this IDistributedApplicationBuilder builder, string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var resource = new DevTunnelResource(name) { TunnelId = $"{name}-{builder.Environment.ApplicationName}" };

        var devTunnel = builder.AddResource(resource)
            .OnInitializeResource(async (r, e, ct) =>
            {
                
            });

        return devTunnel;
    }
}

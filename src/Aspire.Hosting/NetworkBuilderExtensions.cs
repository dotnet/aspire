// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding network resources to an application.
/// </summary>
public static class NetworkBuilderExtensions
{
    /// <summary>
    /// Adds a network resource to the distributed application with the specified expression.
    /// </summary>
    /// <param name="builder">Distributed application builder</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>An <see cref="IResourceBuilder{NetworkResource}"/> instance.</returns>
    /// <remarks>
    /// <example>
    /// <code language="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var frontend = builder.AddNetwork("frontend");
    /// var backend = builder.AddNetwork("backend");
    ///
    /// var database = builder
    ///     .AddContainer("database", "test-database")
    ///     .WithNetwork(backend);
    /// 
    /// var webapi = builder
    ///     .AddProject&lt;Projects.WebApi&gt;("webapi")
    ///     .WithNetwork(frontend)
    ///     .WithNetwork(backend)
    ///     .WithReference(database);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<NetworkResource> AddNetwork(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        var resource = new NetworkResource(name);
        return builder.AddResource(resource)
                      .WithInitialState(new CustomResourceSnapshot
                      {
                          ResourceType = "Network",
                          // TODO: We'll hide this until we come up with a sane representation of these in the dashboard
                          IsHidden = true,
                          Properties = []
                      });
    }

    /// <summary>
    /// Overrides the default network name for this resource. By default Aspire uses <see cref="Resource.Name"/> value of <see cref="NetworkResource"/> when
    /// publishing the network name to the specified deployment. This method allows you to override that behavior with a custom name, but could lead to
    /// naming conflicts if the specified name is not unique.
    /// </summary>
    /// <typeparam name="T">The type of container resource.</typeparam>
    /// <param name="builder">The resource builder for the container resource.</param>
    /// <param name="name">The desired container name. Must be a valid container name or your runtime will report an error.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithNetworkName<T>(this IResourceBuilder<T> builder, string name) where T : NetworkResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        return builder.WithAnnotation(new NetworkNameAnnotation { Name = name }, ResourceAnnotationMutationBehavior.Replace);
    }
}

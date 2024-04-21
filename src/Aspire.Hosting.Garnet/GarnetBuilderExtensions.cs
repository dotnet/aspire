// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Garnet;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Garnet resources to the application model.
/// </summary>
public static class GarnetBuilderExtensions
{
    /// <summary>
    /// Adds a Garnet container to the application model.
    /// </summary>
    /// <example>
    /// <remarks>Use in AspireHost</remarks>
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api") 
    /// var garnet = builder.AddGarnet("MyGarnet"); 
    /// api.WithReference(garnet);
    ///  
    /// builder.Build().Run(); 
    /// </code>
    /// <remarks>Use in Api</remarks>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// var Configuration = builder.Configuration; 
    /// var garnetConnectionString = configure.GetConnectionString("garnet").Split(':');
    ///
    /// using var db = new GarnetClient(garnetConnectionString[0], int.Parse(garnetConnectionString[1]));
    /// await db.ConnectAsync();
    /// var pong = await db.PingAsync();
    /// if (pong != "PONG")
    ///     throw new Exception("PingAsync: Error");
    /// Console.WriteLine("Ping: Success");
    /// </code>
    /// </example>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port to bind the underlying container to.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<GarnetResource> AddGarnet(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        var garnet = new GarnetResource(name);
        return builder.AddResource(garnet)
                      .WithEndpoint(port: port, targetPort: 6379, name: GarnetResource.PrimaryEndpointName)
                      .WithImage(GarnetContainerImageTags.Image, GarnetContainerImageTags.Tag)
                      .WithImageRegistry(GarnetContainerImageTags.Registry);
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Garnet container resource and enables Garnet persistence.
    /// </summary>
    /// <remarks>
    /// Use <see cref="WithPersistence(IResourceBuilder{GarnetResource}, TimeSpan?, long)"/> to adjust Garnet persistence configuration, e.g.:
    /// <code>
    /// var cache = builder.AddGarnet("cache")
    ///                    .WithDataVolume()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </remarks>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">
    /// A flag that indicates if this is a read-only volume. Setting this to <c>true</c> will disable Garnet persistence.<br/>
    /// Defaults to <c>false</c>.
    /// </param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<GarnetResource> WithDataVolume(this IResourceBuilder<GarnetResource> builder, string? name = null, bool isReadOnly = false)
    {
        builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/data", isReadOnly);
        if (!isReadOnly)
        {
            builder.WithPersistence();
        }
        return builder;
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a Garnet container resource and enables Garnet persistence.
    /// </summary>
    /// <example>
    /// <remarks>
    /// Use <see cref="WithPersistence(IResourceBuilder{GarnetResource}, TimeSpan?, long)"/> to adjust Garnet persistence configuration, e.g.:
    /// <code>
    /// var garnet = builder.AddGarnet("garnet")
    ///                    .WithDataBindMount()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </remarks>
    /// </example>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">
    /// A flag that indicates if this is a read-only mount. Setting this to <c>true</c> will disable Garnet persistence.<br/>
    /// Defaults to <c>false</c>.
    /// </param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<GarnetResource> WithDataBindMount(this IResourceBuilder<GarnetResource> builder, string source, bool isReadOnly = false)
    {
        builder.WithBindMount(source, "/data", isReadOnly);
        if (!isReadOnly)
        {
            builder.WithPersistence();
        }
        return builder;
    }

    /// <summary>
    /// Configures a Garnet container resource for persistence.
    /// </summary>
    /// <remarks>
    /// Use with <see cref="WithDataBindMount(IResourceBuilder{GarnetResource}, string, bool)"/>
    /// or <see cref="WithDataVolume(IResourceBuilder{GarnetResource}, string?, bool)"/> to persist Garnet data across sessions with custom persistence configuration, e.g.:
    /// <code>
    /// var cache = builder.AddGarnet("cache")
    ///                    .WithDataVolume()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </remarks>
    /// <param name="builder">The resource builder.</param>
    /// <param name="interval">The interval between snapshot exports. Defaults to 60 seconds.</param>
    /// <param name="keysChangedThreshold">The number of key change operations required to trigger a snapshot at the interval. Defaults to 1.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<GarnetResource> WithPersistence(this IResourceBuilder<GarnetResource> builder, TimeSpan? interval = null, long keysChangedThreshold = 1)
        => builder.WithAnnotation(new GarnetPersistenceCommandLineArgsCallbackAnnotation(interval ?? TimeSpan.FromSeconds(60), keysChangedThreshold), ResourceAnnotationMutationBehavior.Replace);
}

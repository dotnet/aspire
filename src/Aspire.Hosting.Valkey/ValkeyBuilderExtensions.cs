// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Valkey;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Valkey resources to the application model.
/// </summary>
public static class ValkeyBuilderExtensions
{
    private const string ValkeyContainerDataDirectory = "/data";

    /// <summary>
    /// Adds a Valkey container to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port to bind the underlying container to.</param>
    /// <example>
    /// Use in application host
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var valkey = builder.AddValkey("valkey");
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api)
    ///                  .WithReference(valkey);
    ///  
    /// builder.Build().Run(); 
    /// </code>
    /// </example>
    /// <example>
    /// Use in service project with Aspire.StackExchange.Redis package.
    /// <code lang="csharp">
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.AddRedisClient("valkey");
    ///
    /// var multiplexer = builder.Services.BuildServiceProvider()
    ///                                   .GetRequiredService&lt;IConnectionMultiplexer&gt;();
    /// 
    /// var db = multiplexer.GetDatabase();
    /// db.HashSet("key", [new HashEntry("hash", "value")]);
    /// var value = db.HashGet("key", "hash");
    /// </code>
    /// </example>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ValkeyResource> AddValkey(this IDistributedApplicationBuilder builder,
        string name,
        int? port = null)
    {
        var valkey = new ValkeyResource(name);
        return builder.AddResource(valkey)
            .WithEndpoint(port: port, targetPort: 6379, name: ValkeyResource.PrimaryEndpointName)
            .WithImage(ValkeyContainerImageTags.Image, ValkeyContainerImageTags.Tag)
            .WithImageRegistry(ValkeyContainerImageTags.Registry);
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Valkey container resource and enables Valkey persistence.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">
    /// A flag that indicates if this is a read-only volume. Setting this to <c>true</c> will disable Valkey persistence.<br/>
    /// Defaults to <c>false</c>.
    /// </param>
    /// <example>
    /// Use <see cref="WithPersistence(IResourceBuilder{ValkeyResource}, TimeSpan?, long)"/> to adjust Valkey persistence configuration, e.g.:
    /// <code lang="csharp">
    /// var cache = builder.AddValkey("cache")
    ///                    .WithDataVolume()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </example>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ValkeyResource> WithDataVolume(this IResourceBuilder<ValkeyResource> builder,
        string? name = null, bool isReadOnly = false)
    {
        builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), ValkeyContainerDataDirectory,
            isReadOnly);
        if (!isReadOnly)
        {
            builder.WithPersistence();
        }

        return builder;
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a Valkey container resource and enables Valkey persistence.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">
    /// A flag that indicates if this is a read-only mount. Setting this to <c>true</c> will disable Valkey persistence.<br/>
    /// Defaults to <c>false</c>.
    /// </param>
    /// <example>
    /// Use <see cref="WithPersistence(IResourceBuilder{ValkeyResource}, TimeSpan?, long)"/> to adjust Valkey persistence configuration, e.g.:
    /// <code lang="csharp">
    /// var valkey = builder.AddValkey("valkey")
    ///                    .WithDataBindMount()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </example>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ValkeyResource> WithDataBindMount(this IResourceBuilder<ValkeyResource> builder,
        string source, bool isReadOnly = false)
    {
        builder.WithBindMount(source, ValkeyContainerDataDirectory, isReadOnly);
        if (!isReadOnly)
        {
            builder.WithPersistence();
        }

        return builder;
    }

    /// <summary>
    /// Configures a Valkey container resource for persistence.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="interval">The interval between snapshot exports. Defaults to 60 seconds.</param>
    /// <param name="keysChangedThreshold">The number of key change operations required to trigger a snapshot at the interval. Defaults to 1.</param>
    /// <example>
    /// Use with <see cref="WithDataBindMount(IResourceBuilder{ValkeyResource}, string, bool)"/>
    /// or <see cref="WithDataVolume(IResourceBuilder{ValkeyResource}, string?, bool)"/> to persist Valkey data across sessions with custom persistence configuration, e.g.:
    /// <code lang="csharp">
    /// var cache = builder.AddValkey("cache")
    ///                    .WithDataVolume()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </example>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ValkeyResource> WithPersistence(this IResourceBuilder<ValkeyResource> builder,
        TimeSpan? interval = null, long keysChangedThreshold = 1)
        => builder.WithAnnotation(new CommandLineArgsCallbackAnnotation(context =>
        {
            context.Args.Add("--save");
            context.Args.Add((interval ?? TimeSpan.FromSeconds(60)).TotalSeconds.ToString(CultureInfo.InvariantCulture));
            context.Args.Add(keysChangedThreshold.ToString(CultureInfo.InvariantCulture));
            return Task.CompletedTask;
        }), ResourceAnnotationMutationBehavior.Replace);
}

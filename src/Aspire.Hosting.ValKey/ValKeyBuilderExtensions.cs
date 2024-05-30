// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.ValKey;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding ValKey resources to the application model.
/// </summary>
public static class ValKeyBuilderExtensions
{
    private const string ValKeyContainerDataDirectory = "/data";

    /// <summary>
    /// Adds a ValKey container to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port to bind the underlying container to.</param>
    /// <example>
    /// Use in application host
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var valKey = builder.AddValKey("valKey");
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api)
    ///                  .WithReference(valKey);
    ///  
    /// builder.Build().Run(); 
    /// </code>
    /// </example>
    /// <example>
    /// Use in Api with Aspire.StackExchange.Redis
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.AddRedisClient("valKey");
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
    public static IResourceBuilder<ValKeyResource> AddValKey(this IDistributedApplicationBuilder builder,
        string name,
        int? port = null)
    {
        var valKey = new ValKeyResource(name);
        return builder.AddResource(valKey)
            .WithEndpoint(port: port, targetPort: 6379, name: ValKeyResource.PrimaryEndpointName)
            .WithImage(ValKeyContainerImageTags.Image, ValKeyContainerImageTags.Tag)
            .WithImageRegistry(ValKeyContainerImageTags.Registry);
    }

    /// <summary>
    /// Adds a named volume for the data folder to a ValKey container resource and enables ValKey persistence.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">
    /// A flag that indicates if this is a read-only volume. Setting this to <c>true</c> will disable ValKey persistence.<br/>
    /// Defaults to <c>false</c>.
    /// </param>
    /// <example>
    /// Use <see cref="WithPersistence(IResourceBuilder{ValKeyResource}, TimeSpan?, long)"/> to adjust ValKey persistence configuration, e.g.:
    /// <code>
    /// var cache = builder.AddValKey("cache")
    ///                    .WithDataVolume()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </example>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ValKeyResource> WithDataVolume(this IResourceBuilder<ValKeyResource> builder,
        string? name = null, bool isReadOnly = false)
    {
        builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), ValKeyContainerDataDirectory,
            isReadOnly);
        if (!isReadOnly)
        {
            builder.WithPersistence();
        }

        return builder;
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a ValKey container resource and enables ValKey persistence.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">
    /// A flag that indicates if this is a read-only mount. Setting this to <c>true</c> will disable ValKey persistence.<br/>
    /// Defaults to <c>false</c>.
    /// </param>
    /// <example>
    /// Use <see cref="WithPersistence(IResourceBuilder{ValKeyResource}, TimeSpan?, long)"/> to adjust ValKey persistence configuration, e.g.:
    /// <code>
    /// var valKey = builder.AddValKey("valKey")
    ///                    .WithDataBindMount()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </example>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ValKeyResource> WithDataBindMount(this IResourceBuilder<ValKeyResource> builder,
        string source, bool isReadOnly = false)
    {
        builder.WithBindMount(source, ValKeyContainerDataDirectory, isReadOnly);
        if (!isReadOnly)
        {
            builder.WithPersistence();
        }

        return builder;
    }

    /// <summary>
    /// Configures a ValKey container resource for persistence.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="interval">The interval between snapshot exports. Defaults to 60 seconds.</param>
    /// <param name="keysChangedThreshold">The number of key change operations required to trigger a snapshot at the interval. Defaults to 1.</param>
    /// <example>
    /// Use with <see cref="WithDataBindMount(IResourceBuilder{ValKeyResource}, string, bool)"/>
    /// or <see cref="WithDataVolume(IResourceBuilder{ValKeyResource}, string?, bool)"/> to persist ValKey data across sessions with custom persistence configuration, e.g.:
    /// <code>
    /// var cache = builder.AddValKey("cache")
    ///                    .WithDataVolume()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </example>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ValKeyResource> WithPersistence(this IResourceBuilder<ValKeyResource> builder,
        TimeSpan? interval = null, long keysChangedThreshold = 1)
        => builder.WithAnnotation(new CommandLineArgsCallbackAnnotation(context =>
        {
            context.Args.Add("--save");
            context.Args.Add((interval ?? TimeSpan.FromSeconds(60)).TotalSeconds.ToString(CultureInfo.InvariantCulture));
            context.Args.Add(keysChangedThreshold.ToString(CultureInfo.InvariantCulture));
            return Task.CompletedTask;
        }), ResourceAnnotationMutationBehavior.Replace);
}

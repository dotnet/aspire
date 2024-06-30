using System.Globalization;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Utils.Cache;

/// <summary>
/// Provides extension methods for adding cache resources to the application model.
/// </summary>
public static class CacheBuilderExtensions
{
    /// <summary>
    /// Adds a Cache container to the application model.
    /// </summary>
    /// <example>
    /// Use in application host
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var cache = builder.AddCache("cache");
    /// var api = builder.AddProject{Projects.Api}("api")
    ///                  .WithReference(cache);
    ///  
    /// builder.Build().Run(); 
    /// </code>
    /// </example>
    /// <example>
    /// Use in Api with Aspire.StackExchange.Redis
    /// <code lang="csharp">
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.AddRedisClient("cache");
    ///
    /// var multiplexer = builder.Services.BuildServiceProvider()
    ///                                   .GetRequiredService{IConnectionMultiplexer}();
    /// 
    /// var db = multiplexer.GetDatabase();
    /// db.HashSet("key", [new HashEntry("hash", "value")]);
    /// var value = db.HashGet("key", "hash");
    /// </code>
    /// </example>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="cacheResource"></param>  //The name of the resource. This name will be used as the connection string name when referenced in a dependency.
    /// <param name="cacheContainerImageTags"></param>
    /// <param name="targetPort"></param>
    /// <param name="port">The host port to bind the underlying container to.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> AddCache<T>(this IDistributedApplicationBuilder builder,
        T cacheResource,
        ICacheContainerImageTags cacheContainerImageTags,
        int targetPort,
        int? port = null)
        where T : CacheResource
        => builder.AddResource(cacheResource)
            .WithEndpoint(port: port, targetPort: targetPort, name: CacheResource.PrimaryEndpointName)
            .WithImage(cacheContainerImageTags.GetImage(), cacheContainerImageTags.GetTag())
            .WithImageRegistry(cacheContainerImageTags.GetRegistry());

    /// <summary>
    /// Adds a named volume for the data folder to a Cache container resource and enables Cache persistence.
    /// </summary>
    /// <example>
    /// Use <see cref="WithPersistence{T}(IResourceBuilder{T}, TimeSpan?, long)"/> to adjust Cache persistence configuration, e.g.:
    /// <code lang="csharp">
    /// var cache = builder.AddCache("cache")
    ///                    .WithDataVolume()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </example>
    /// <param name="builder">The resource builder.</param>
    /// <param name="target"></param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">
    /// A flag that indicates if this is a read-only volume. Setting this to <c>true</c> will disable Cache persistence.<br/>
    /// Defaults to <c>false</c>.
    /// </param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithDataVolume<T>(this IResourceBuilder<T> builder,
        string target,
        string? name = null,
        bool isReadOnly = false)
    where T : CacheResource
    {
        builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), target,
            isReadOnly);
        if (!isReadOnly)
        {
            builder.WithPersistence();
        }

        return builder;
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a Cache container resource and enables Cache persistence.
    /// </summary>
    /// <example>
    /// Use <see cref="WithPersistence{T}(IResourceBuilder{T}, TimeSpan?, long)"/> to adjust Cache persistence configuration, e.g.:
    /// <code lang="csharp">
    /// var cache = builder.AddCache("cache")
    ///                    .WithDataBindMount()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </example>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="target"></param>
    /// <param name="isReadOnly">
    /// A flag that indicates if this is a read-only mount. Setting this to <c>true</c> will disable Cache persistence.<br/>
    /// Defaults to <c>false</c>.
    /// </param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithDataBindMount<T>(this IResourceBuilder<T> builder,
        string source,
        string target,
        bool isReadOnly = false)
    where T : CacheResource
    {
        builder.WithBindMount(source, target, isReadOnly);
        if (!isReadOnly)
        {
            builder.WithPersistence();
        }

        return builder;
    }

    /// <summary>
    /// Configures a Cache container resource for persistence.
    /// </summary>
    /// <example>
    /// Use with <see cref="WithDataBindMount{T}(IResourceBuilder{T}, string, string, bool)"/>
    /// or <see cref="WithDataVolume{T}(IResourceBuilder{T}, string, string?, bool)"/> to persist Cache data across sessions with custom persistence configuration, e.g.:
    /// <code lang="csharp">
    /// var cache = builder.AddCache("cache")
    ///                    .WithDataVolume()
    ///                    .WithPersistence(TimeSpan.FromSeconds(10), 5);
    /// </code>
    /// </example>
    /// <param name="builder">The resource builder.</param>
    /// <param name="interval">The interval between snapshot exports. Defaults to 60 seconds.</param>
    /// <param name="keysChangedThreshold">The number of key change operations required to trigger a snapshot at the interval. Defaults to 1.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithPersistence<T>(this IResourceBuilder<T> builder,
        TimeSpan? interval = null,
        long keysChangedThreshold = 1)
    where T : CacheResource
        => builder.WithAnnotation(new CommandLineArgsCallbackAnnotation(context =>
        {
            context.Args.Add("--save");
            context.Args.Add((interval ?? TimeSpan.FromSeconds(60)).TotalSeconds.ToString(CultureInfo.InvariantCulture));
            context.Args.Add(keysChangedThreshold.ToString(CultureInfo.InvariantCulture));
            return Task.CompletedTask;
        }), ResourceAnnotationMutationBehavior.Replace);
}

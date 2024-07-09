using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Utils;

internal static class RedisCommander
{
    private const string Registry = "docker.io";
    private const string Image = "rediscommander/redis-commander";
    private const string Tag = "latest";

    /// <summary>
    /// Configures a container resource for Redis Commander which is pre-configured to connect to the <see cref="ContainerResource"/> that this method is used on.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the <see cref="ContainerResource"/>.</param>
    /// <param name="containerResource"></param>
    /// <returns></returns>
    public static IResourceBuilder<TRedisCommander> WithRedisCommander<TCache, TRedisCommander>(
        this IResourceBuilder<TCache> builder,
        TRedisCommander containerResource)
    where TCache : ContainerResource
    where TRedisCommander : ContainerResource
    => builder.ApplicationBuilder.AddResource(containerResource)
        .WithImage(Image, Tag)
        .WithImageRegistry(Registry)
        .WithHttpEndpoint(targetPort: 8081, name: "http")
        .ExcludeFromManifest();

    /// <summary>
    /// Configures the host port that the Redis Commander resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for Redis Commander.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    /// <returns>The resource builder for PGAdmin.</returns>
    public static IResourceBuilder<T> WithHostPort<T>(this IResourceBuilder<T> builder, int? port)
    where T : ContainerResource
    {
        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }
}

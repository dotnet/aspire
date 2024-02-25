// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Redis resources to the application model.
/// </summary>
public static class AzureRedisExtensions
{
    /// <summary>
    /// Configures the resource to be published as Azure Cache for Redis when deployed via Azure Developer CLI.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{RedisResource}"/> builder.</param>
    /// <param name="callback">Callback to configure Azure resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{RedisResource}"/> builder.</returns>
    public static IResourceBuilder<RedisResource> PublishAsAzureRedis(this IResourceBuilder<RedisResource> builder, Action<IResourceBuilder<AzureRedisResource>>? callback = null)
    {
        var resource = new AzureRedisResource(builder.Resource);
        var azureRedisBuilder = builder.ApplicationBuilder.CreateResourceBuilder(resource).ConfigureDefaults();

        if (callback != null)
        {
            callback(azureRedisBuilder);
        }

        return builder;
    }

    /// <summary>
    /// Configures resource to use Azure for local development and when doing a deployment via the Azure Developer CLI.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{RedisResource}"/> builder.</param>
    /// <param name="callback">Callback to configure Azure resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{RedisResource}"/> builder.</returns>
    public static IResourceBuilder<RedisResource> AsAzureRedis(this IResourceBuilder<RedisResource> builder, Action<IResourceBuilder<AzureRedisResource>>? callback = null)
    {
        var resource = new AzureRedisResource(builder.Resource);
        var azureRedisBuilder = builder.ApplicationBuilder.CreateResourceBuilder(resource).ConfigureDefaults();

        // Used to hold a reference to the azure surrogate for use with the provisioner.
        builder.WithAnnotation(new AzureBicepResourceAnnotation(resource));
        builder.WithConnectionStringRedirection(resource);

        // Remove the container annotation so that DCP doesn't do anything with it.
        if (builder.Resource.Annotations.OfType<ContainerImageAnnotation>().SingleOrDefault() is { } containerAnnotation)
        {
            builder.Resource.Annotations.Remove(containerAnnotation);
        }

        if (callback != null)
        {
            callback(azureRedisBuilder);
        }

        return builder;
    }

    private static IResourceBuilder<AzureRedisResource> ConfigureDefaults(this IResourceBuilder<AzureRedisResource> builder)
    {
        var resource = builder.Resource;
        return builder.WithParameter("redisCacheName", resource.CreateBicepResourceName())
                      .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}

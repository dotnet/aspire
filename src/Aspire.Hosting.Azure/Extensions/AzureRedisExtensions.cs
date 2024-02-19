// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Bicep;
using Aspire.Hosting.Azure.Redis;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Redis resources to the application model.
/// </summary>
public static class AzureRedisExtensions
{
    /// <summary>
    /// Publishes the Azure Redis resource to the manifest.
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
    /// Used in conjunction with the Azure Provisioner to provision an Azure Redis resource for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{RedisResource}"/> builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{RedisResource}"/> builder.</returns>
    public static IResourceBuilder<RedisResource> RunAsAzureRedis(this IResourceBuilder<RedisResource> builder)
    {
        var resource = new AzureRedisResource(builder.Resource);
        builder.ApplicationBuilder.CreateResourceBuilder(resource).ConfigureDefaults();

        // Used to hold a reference to the azure surrogate for use with the provisioner.
        builder.WithAnnotation(new AzureBicepResourceAnnotation(resource));

        // Remove the container annotation so that DCP doesn't do anything with it.
        if (builder.Resource.Annotations.OfType<ContainerImageAnnotation>().SingleOrDefault() is { } containerAnnotation)
        {
            builder.Resource.Annotations.Remove(containerAnnotation);
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

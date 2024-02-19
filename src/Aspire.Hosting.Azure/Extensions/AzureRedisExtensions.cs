// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
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
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="callback">The callback to configure the underlying Azure resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<RedisResource> PublishAsAzureRedis(this IResourceBuilder<RedisResource> builder, Action<AzureRedisResource>? callback = null)
    {
        var resource = new AzureRedisResource(builder.Resource);
        builder.ApplicationBuilder.CreateResourceBuilder(resource).ConfigureDefaults();

        if (callback != null)
        {
            callback(resource);
        }

        return builder.WithManifestPublishingCallback(resource.WriteToManifest);
    }

    private static IResourceBuilder<AzureRedisResource> ConfigureDefaults(this IResourceBuilder<AzureRedisResource> builder)
    {
        var resource = builder.Resource;
        return builder.WithParameter("redisCacheName", resource.CreateBicepResourceName())
                      .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}

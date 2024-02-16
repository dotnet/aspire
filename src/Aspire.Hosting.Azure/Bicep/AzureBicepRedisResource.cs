// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Redis resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureBicepRedisResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.redis.bicep"),
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Redis resource.
    /// </summary>
    public string ConnectionStringExpression =>
        $"{{{Name}.secretOutputs.connectionString}}";

    /// <summary>
    /// Gets the connection string for the Azure Redis resource.
    /// </summary>
    /// <returns>The connection string for the Azure Redis resource.</returns>
    public string? GetConnectionString()
    {
        return SecretOutputs["connectionString"];
    }
}

/// <summary>
/// Provides extension methods for adding the Azure Redis resources to the application model.
/// </summary>
public static class AzureBicepRedisExtensions
{
    /// <summary>
    /// Adds an Azure Redis resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureBicepRedisResource> AddBicepAzureRedis(this IDistributedApplicationBuilder builder, string name)
    {
        return builder.AddResource(new AzureBicepRedisResource(name)).ConfigureDefaults();
    }

    /// <summary>
    /// Publishes the Azure Redis resource to the manifest.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<RedisResource> PublishAsAzureRedis(this IResourceBuilder<RedisResource> builder)
    {
        var resource = new AzureBicepRedisResource(builder.Resource.Name);
        builder.ApplicationBuilder.CreateResourceBuilder(resource).ConfigureDefaults();
        return builder.WithManifestPublishingCallback(resource.WriteToManifest);
    }

    private static IResourceBuilder<AzureBicepRedisResource> ConfigureDefaults(this IResourceBuilder<AzureBicepRedisResource> builder)
    {
        var resource = builder.Resource;
        return builder.WithParameter("redisCacheName", resource.CreateBicepResourceName())
                      .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}

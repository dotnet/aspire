// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.KeyVaults;
using Azure.Provisioning.Redis;

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

    /// <summary>
    /// Configures the resource to be published as Azure Cache for Redis when deployed via Azure Developer CLI.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{RedisResource}"/> builder.</param>
    /// <param name="configureResource">Callback to configure Azure resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{RedisResource}"/> builder.</returns>
    public static IResourceBuilder<RedisResource> PublishAsAzureRedisConstruct(this IResourceBuilder<RedisResource> builder, Action<ResourceModuleConstruct, RedisCache>? configureResource = null)
    {
        return builder.PublishAsAzureRedisConstruct(configureResource);
    }

    /// <summary>
    /// Configures the resource to be published as Azure Cache for Redis when deployed via Azure Developer CLI.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{RedisResource}"/> builder.</param>
    /// <param name="configureResource">Callback to configure Azure resource.</param>
    /// <param name="useProvisioner"></param>
    /// <returns>A reference to the <see cref="IResourceBuilder{RedisResource}"/> builder.</returns>
    internal static IResourceBuilder<RedisResource> PublishAsAzureRedisConstruct(this IResourceBuilder<RedisResource> builder, Action<ResourceModuleConstruct, RedisCache>? configureResource = null, bool useProvisioner = false)
    {
        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var redisCache = new RedisCache(construct, name: builder.Resource.Name);

            // EXPERIMENTAL: Create a keyvault using the same details as the existing one.
            var vaultNameParameter = new Parameter("keyVaultName");
            construct.AddParameter(vaultNameParameter);

            var keyVault = new KeyVault(construct);
            keyVault.AssignProperty(x => x.Name, "keyVaultName");

            // HACK: Temporary comment out whilst integration with AZD is figured out.
            //var role = keyVault.AssignRole(RoleDefinition.KeyVaultAdministrator);
            //role.AssignProperty(x => x.PrincipalId, construct.PrincipalIdParameter);
            //role.AssignProperty(x => x.PrincipalType, construct.PrincipalTypeParameter);

            // HACK: Use the name {resourcename}.salt1 to ensure the Bicep construct name is unique.
            var keyVaultSecret = new KeyVaultSecret(construct, $"{builder.Resource.Name}salt1");
            keyVaultSecret.AssignProperty(x => x.Name, "'connectionString'");
            keyVaultSecret.AssignProperty(
                x => x.Properties.Value,
                $$"""'${{{redisCache.Name}}.properties.hostName},ssl=true,password=${{{redisCache.Name}}.listKeys({{redisCache.Name}}.apiVersion).primaryKey}'"""
                );

            // HACK: Use the name {resourcename}.salt1 to ensure the Bicep construct name is unique.
            var discardSecret = new KeyVaultSecret(construct, $"{builder.Resource.Name}salt2");
            discardSecret.AssignProperty(x => x.Name, "'keyVaultName'");
            discardSecret.AssignProperty(x => x.Properties.Value, vaultNameParameter); // HACK: Ensures parameter stays in Bicep!

            // HACK: Use the name {resourcename}.salt1 to ensure the Bicep construct name is unique.
            var discardPrincipalId = new KeyVaultSecret(construct, $"{builder.Resource.Name}salt3");
            discardPrincipalId.AssignProperty(x => x.Name, "'principalId'");
            discardPrincipalId.AssignProperty(x => x.Properties.Value, construct.PrincipalIdParameter); // HACK: Ensures parameter stays in Bicep!

            // HACK: Use the name {resourcename}.salt1 to ensure the Bicep construct name is unique.
            var discardPrincipalType = new KeyVaultSecret(construct, $"{builder.Resource.Name}salt4");
            discardPrincipalType.AssignProperty(x => x.Name, "'principalType'");
            discardPrincipalType.AssignProperty(x => x.Properties.Value, construct.PrincipalTypeParameter); // HACK: Ensures parameter stays in Bicep!

            if (configureResource != null)
            {
                configureResource(construct, redisCache);
            }
        };

        var resource = new AzureRedisConstructResource(builder.Resource, configureConstruct);
        var resourceBuilder = builder.ApplicationBuilder.CreateResourceBuilder(resource)
                                     .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                                     .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
                                     .WithManifestPublishingCallback(resource.WriteToManifest);

        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
            {
                resourceBuilder.WithParameter(AzureBicepResource.KnownParameters.PrincipalType);
            }
        }

        if (useProvisioner)
        {
            // Used to hold a reference to the azure surrogate for use with the provisioner.
            builder.WithAnnotation(new AzureBicepResourceAnnotation(resource));
            builder.WithConnectionStringRedirection(resource);

            // Remove the container annotation so that DCP doesn't do anything with it.
            if (builder.Resource.Annotations.OfType<ContainerImageAnnotation>().SingleOrDefault() is { } containerAnnotation)
            {
                builder.Resource.Annotations.Remove(containerAnnotation);
            }
        }

        return builder;
    }

    /// <summary>
    /// Configures resource to use Azure for local development and when doing a deployment via the Azure Developer CLI.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{RedisResource}"/> builder.</param>
    /// <param name="configureResource">Callback to configure Azure resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{RedisResource}"/> builder.</returns>
    public static IResourceBuilder<RedisResource> AsAzureRedisConstruct(this IResourceBuilder<RedisResource> builder, Action<ResourceModuleConstruct, RedisCache>? configureResource = null)
    {
        return builder.PublishAsAzureRedisConstruct(configureResource, useProvisioner: true);
    }
}

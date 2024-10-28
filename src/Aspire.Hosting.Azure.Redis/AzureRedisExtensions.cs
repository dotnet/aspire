// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using RedisResource = Aspire.Hosting.ApplicationModel.RedisResource;
using CdkRedisResource = Azure.Provisioning.Redis.RedisResource;
using Azure.Provisioning.Redis;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Primitives;

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
    /// <returns>A reference to the <see cref="IResourceBuilder{RedisResource}"/> builder.</returns>
    [Obsolete($"This method is obsolete and will be removed in a future version. Use {nameof(AddAzureRedis)} instead to add an Azure Cache for Redis resource.")]
    public static IResourceBuilder<RedisResource> PublishAsAzureRedis(this IResourceBuilder<RedisResource> builder)
    {
        return builder.PublishAsAzureRedisInternal(useProvisioner: false);
    }

    [Obsolete]
    private static IResourceBuilder<RedisResource> PublishAsAzureRedisInternal(this IResourceBuilder<RedisResource> builder, bool useProvisioner)
    {
        builder.ApplicationBuilder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var kvNameParam = new ProvisioningParameter("keyVaultName", typeof(string));
            infrastructure.Add(kvNameParam);

            var keyVault = KeyVaultService.FromExisting("keyVault");
            keyVault.Name = kvNameParam;
            infrastructure.Add(keyVault);

            var redisCache = CreateRedisResource(infrastructure);

            var secret = new KeyVaultSecret("connectionString")
            {
                Parent = keyVault,
                Name = "connectionString",
                Properties = new SecretProperties
                {
                    Value = BicepFunction.Interpolate($"{redisCache.HostName},ssl=true,password={redisCache.GetKeys().PrimaryKey}")
                }
            };
            infrastructure.Add(secret);
        };

        var resource = new AzureRedisResource(builder.Resource, configureInfrastructure);
        var resourceBuilder = builder.ApplicationBuilder.CreateResourceBuilder(resource)
                                     .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
                                     .WithManifestPublishingCallback(resource.WriteToManifest);

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
    /// <returns>A reference to the <see cref="IResourceBuilder{RedisResource}"/> builder.</returns>
    [Obsolete($"This method is obsolete and will be removed in a future version. Use {nameof(AddAzureRedis)} instead to add an Azure Cache for Redis resource.")]
    public static IResourceBuilder<RedisResource> AsAzureRedis(this IResourceBuilder<RedisResource> builder)
    {
        return builder.PublishAsAzureRedisInternal(useProvisioner: true);
    }

    /// <summary>
    /// Adds an Azure Cache for Redis resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureRedisCacheResource}"/> builder.</returns>
    /// <remarks>
    /// By default, the Azure Cache for Redis resource is configured to use Microsoft Entra ID (Azure Active Directory) for authentication.
    /// This requires changes to the application code to use an azure credential to authenticate with the resource. See
    /// https://github.com/Azure/Microsoft.Azure.StackExchangeRedis for more information.
    /// 
    /// You can use the <see cref="WithAccessKeyAuthentication"/> method to configure the resource to use access key authentication.
    /// </remarks>
    /// <example>
    /// The following example creates an Azure Cache for Redis resource and referencing that resource in a .NET project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var cache = builder.AddAzureRedis("cache");
    ///
    /// builder.AddProject&lt;Projects.ProductService&gt;()
    ///     .WithReference(cache);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<AzureRedisCacheResource> AddAzureRedis(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name)
    {
        builder.AddAzureProvisioning();

        var configureInfrastructure = static (AzureResourceInfrastructure infrastructure) =>
        {
            var redis = CreateRedisResource(infrastructure);

            redis.RedisConfiguration = new RedisCommonConfiguration()
            {
                IsAadEnabled = "true"
            };
            redis.IsAccessKeyAuthenticationDisabled = true;

            var principalIdParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalId, typeof(string));
            var principalNameParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalName, typeof(string));
            infrastructure.Add(new RedisCacheAccessPolicyAssignment($"{redis.BicepIdentifier}_contributor")
            {
                Parent = redis,
                AccessPolicyName = "Data Contributor",
                ObjectId = principalIdParameter,
                ObjectIdAlias = principalNameParameter
            });

            infrastructure.Add(new ProvisioningOutput("connectionString", typeof(string))
            {
                Value = BicepFunction.Interpolate($"{redis.HostName},ssl=true")
            });
        };

        var resource = new AzureRedisCacheResource(name, configureInfrastructure);
        return builder.AddResource(resource)
            .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
            .WithParameter(AzureBicepResource.KnownParameters.PrincipalName)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Configures an Azure Cache for Redis resource to run locally in a container.
    /// </summary>
    /// <param name="builder">The Azure Cache for Redis resource builder.</param>
    /// <param name="configureContainer">Callback that exposes underlying container to allow for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureRedisCacheResource}"/> builder.</returns>
    /// <example>
    /// The following example creates an Azure Cache for Redis resource that runs locally in a
    /// Redis container and referencing that resource in a .NET project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var cache = builder.AddAzureRedis("cache")
    ///     .RunAsContainer();
    ///
    /// builder.AddProject&lt;Projects.ProductService&gt;()
    ///     .WithReference(cache);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<AzureRedisCacheResource> RunAsContainer(
        this IResourceBuilder<AzureRedisCacheResource> builder,
        Action<IResourceBuilder<RedisResource>>? configureContainer = null)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        var azureResource = builder.Resource;
        builder.ApplicationBuilder.Resources.Remove(azureResource);

        var redisContainer = builder.ApplicationBuilder.AddRedis(azureResource.Name);

        azureResource.SetInnerResource(redisContainer.Resource);

        configureContainer?.Invoke(redisContainer);

        return builder;
    }

    /// <summary>
    /// Configures the resource to use access key authentication for Azure Cache for Redis.
    /// </summary>
    /// <param name="builder">The Azure Cache for Redis resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureRedisCacheResource}"/> builder.</returns>
    /// <example>
    /// The following example creates an Azure Cache for Redis resource that uses access key authentication.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var cache = builder.AddAzureRedis("cache")
    ///     .WithAccessKeyAuthentication();
    ///
    /// builder.AddProject&lt;Projects.ProductService&gt;()
    ///     .WithReference(cache);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<AzureRedisCacheResource> WithAccessKeyAuthentication(
        this IResourceBuilder<AzureRedisCacheResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var azureResource = builder.Resource;
        azureResource.ConnectionStringSecretOutput = new BicepSecretOutputReference("connectionString", azureResource);

        return builder
           .RemoveActiveDirectoryParameters()
           .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
           .ConfigureInfrastructure(infrastructure =>
           {
               RemoveActiveDirectoryAuthResources(infrastructure);

               var redis = infrastructure.GetProvisionableResources().OfType<CdkRedisResource>().FirstOrDefault(r => r.BicepIdentifier == builder.Resource.GetBicepIdentifier())
                   ?? throw new InvalidOperationException($"Could not find a RedisResource with name {builder.Resource.Name}.");

               var kvNameParam = new ProvisioningParameter("keyVaultName", typeof(string));
               infrastructure.Add(kvNameParam);

               var keyVault = KeyVaultService.FromExisting("keyVault");
               keyVault.Name = kvNameParam;
               infrastructure.Add(keyVault);

               redis.RedisConfiguration.IsAadEnabled.ClearValue();
               redis.IsAccessKeyAuthenticationDisabled.ClearValue();

               var secret = new KeyVaultSecret("connectionString")
               {
                   Parent = keyVault,
                   Name = "connectionString",
                   Properties = new SecretProperties
                   {
                       Value = BicepFunction.Interpolate($"{redis.HostName},ssl=true,password={redis.GetKeys().PrimaryKey}")
                   }
               };
               infrastructure.Add(secret);
           });
    }

    private static CdkRedisResource CreateRedisResource(AzureResourceInfrastructure Infrastructure)
    {
        var redisCache = new CdkRedisResource(Infrastructure.AspireResource.GetBicepIdentifier())
        {
            Sku = new RedisSku()
            {
                Name = RedisSkuName.Basic,
                Family = RedisSkuFamily.BasicOrStandard,
                Capacity = 1
            },
            EnableNonSslPort = false,
            MinimumTlsVersion = RedisTlsVersion.Tls1_2,
            Tags = { { "aspire-resource-name", Infrastructure.AspireResource.Name } }
        };
        Infrastructure.Add(redisCache);

        return redisCache;
    }

    private static IResourceBuilder<AzureRedisCacheResource> RemoveActiveDirectoryParameters(
        this IResourceBuilder<AzureRedisCacheResource> builder)
    {
        builder.Resource.Parameters.Remove(AzureBicepResource.KnownParameters.PrincipalId);
        builder.Resource.Parameters.Remove(AzureBicepResource.KnownParameters.PrincipalName);
        return builder;
    }

    private static void RemoveActiveDirectoryAuthResources(AzureResourceInfrastructure infrastructure)
    {
        var resourcesToRemove = new List<Provisionable>();
        foreach (var resource in infrastructure.GetProvisionableResources())
        {
            if (resource is RedisCacheAccessPolicyAssignment accessPolicy &&
                accessPolicy.BicepIdentifier == $"{infrastructure.AspireResource.GetBicepIdentifier()}_contributor")
            {
                resourcesToRemove.Add(resource);
            }
            else if (resource is ProvisioningOutput output && output.BicepIdentifier == "connectionString")
            {
                resourcesToRemove.Add(resource);
            }
        }

        foreach (var resourceToRemove in resourcesToRemove)
        {
            infrastructure.Remove(resourceToRemove);
        }
    }
}

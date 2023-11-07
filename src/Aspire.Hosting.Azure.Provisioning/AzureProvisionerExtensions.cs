// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Data.Cosmos;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Lifecycle;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.Redis;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.ServiceBus;
using Azure.ResourceManager.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding support for generating Azure resources dynamically during application startup.
/// </summary>
public static class AzureProvisionerExtensions
{
    /// <summary>
    /// Adds support for generating azure resources dynamically during application startup.
    /// The application must configure the appropriate subscription, location.
    /// </summary>
    public static IDistributedApplicationBuilder AddAzureProvisioning(this IDistributedApplicationBuilder builder)
    {
        builder.Services.AddLifecycleHook<AzureProvisioner>();

        // Attempt to read azure configuration from configuration
        builder.Services.AddOptions<AzureProvisionerOptions>()
            .BindConfiguration("Azure");

        // We're adding 2 because there's no easy way to enumerate all keys and all service types
        builder.AddAzureProvisioner<AzureKeyVaultResource, KeyVaultProvisioner>();
        builder.AddResourceEnumerator(resourceGroup => resourceGroup.GetKeyVaults(), resource => resource.Data.Tags);

        builder.AddAzureProvisioner<AzureStorageResource, StorageProvisioner>();
        builder.AddResourceEnumerator(resourceGroup => resourceGroup.GetStorageAccounts(), resource => resource.Data.Tags);

        builder.AddAzureProvisioner<AzureServiceBusResource, ServiceBusProvisioner>();
        builder.AddResourceEnumerator(resourceGroup => resourceGroup.GetServiceBusNamespaces(), resource => resource.Data.Tags);

        builder.AddAzureProvisioner<AzureRedisResource, AzureRedisProvisioner>();
        builder.AddResourceEnumerator(resourceGroup => resourceGroup.GetAllRedis(), resource => resource.Data.Tags);

        builder.AddAzureProvisioner<AzureCosmosDBResource, AzureCosmosDBProvisioner>();
        builder.AddResourceEnumerator(resourceGroup => resourceGroup.GetCosmosDBAccounts(), resource => resource.Data.Tags);

        return builder;
    }

    internal static IDistributedApplicationBuilder AddAzureProvisioner<TResource, TProvisioner>(this IDistributedApplicationBuilder builder)
        where TResource : IAzureResource
        where TProvisioner : AzureResourceProvisioner<TResource>
    {
        // This lets us avoid using open generics in the caller, we can use keyed lookup instead
        builder.Services.AddKeyedSingleton<IAzureResourceProvisioner, TProvisioner>(typeof(TResource));
        return builder;
    }

    internal static IDistributedApplicationBuilder AddResourceEnumerator<TResource>(this IDistributedApplicationBuilder builder,
        Func<ResourceGroupResource, IAsyncEnumerable<TResource>> getResources,
        Func<TResource, IDictionary<string, string>> getTags)
        where TResource : ArmResource
    {
        builder.Services.AddSingleton<IAzureResourceEnumerator>(new AzureResourceEnumerator<TResource>(getResources, getTags));
        return builder;
    }
}

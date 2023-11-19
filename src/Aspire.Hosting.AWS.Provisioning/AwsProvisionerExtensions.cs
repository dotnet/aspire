// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CloudFormation;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.Provisioning;
using Aspire.Hosting.AWS.Provisioning.Provisioners;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Lifecycle;
using LocalStack.Client.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding support for generating Azure resources dynamically during application startup.
/// </summary>
public static class AwsProvisionerExtensions
{
    /// <summary>
    /// Adds support for generating azure resources dynamically during application startup.
    /// The application must configure the appropriate subscription, location.
    /// </summary>
    public static IDistributedApplicationBuilder AddAwsProvisioning(this IDistributedApplicationBuilder builder)
    {
        builder.Services.AddLifecycleHook<AwsProvisioner>();

        builder.Services.AddLocalStack(builder.Configuration);
        builder.Services.AddAwsService<IAmazonCloudFormation>();

        // Attempt to read aws configuration from configuration
        builder.Services.AddOptions<AwsProvisionerOptions>()
            .BindConfiguration("AWS");

        // TODO: We're keeping state in the provisioners, which is not ideal
        builder.Services.AddKeyedTransient<IAwsResourceProvisioner, S3Provisioner>(typeof(AwsS3BucketResource));

        // We're adding 2 because there's no easy way to enumerate all keys and all service types
        //builder.AddAzureProvisioner<AzureKeyVaultResource, KeyVaultProvisioner>();
        //builder.AddResourceEnumerator(resourceGroup => resourceGroup.GetKeyVaults(), resource => resource.Data.Tags);

        //builder.AddAzureProvisioner<AzureStorageResource, StorageProvisioner>();
        //builder.AddResourceEnumerator(resourceGroup => resourceGroup.GetStorageAccounts(), resource => resource.Data.Tags);

        //builder.AddAzureProvisioner<AzureServiceBusResource, ServiceBusProvisioner>();
        //builder.AddResourceEnumerator(resourceGroup => resourceGroup.GetServiceBusNamespaces(), resource => resource.Data.Tags);

        //builder.AddAzureProvisioner<AzureRedisResource, AzureRedisProvisioner>();
        //builder.AddResourceEnumerator(resourceGroup => resourceGroup.GetAllRedis(), resource => resource.Data.Tags);

        //builder.AddAzureProvisioner<AzureAppConfigurationResource, AppConfigurationProvisioner>();
        //builder.AddResourceEnumerator(resourceGroup => resourceGroup.GetAppConfigurationStores(), resource => resource.Data.Tags);

        //builder.AddAzureProvisioner<AzureCosmosDBResource, AzureCosmosDBProvisioner>();
        //builder.AddResourceEnumerator(resourceGroup => resourceGroup.GetCosmosDBAccounts(), resource => resource.Data.Tags);

        //builder.AddAzureProvisioner<AzureSqlServerResource, SqlServerProvisioner>();
        //builder.AddResourceEnumerator(resourceGroup => resourceGroup.GetSqlServers(), resource => resource.Data.Tags);

        return builder;
    }

    // internal static IDistributedApplicationBuilder AddAwsProvisioner<TResource, TProvisioner>(this IDistributedApplicationBuilder builder)
    //     where TResource : class, IAwsResource
    //     where TProvisioner : AwsResourceProvisioner<TResource, AwsConstruct>
    // {
    //     // This lets us avoid using open generics in the caller, we can use keyed lookup instead
    //     builder.Services.AddKeyedSingleton<IAwsResourceProvisioner, TProvisioner>(typeof(TResource));
    //     return builder;
    // }

    //internal static IDistributedApplicationBuilder AddResourceEnumerator<TResource>(this IDistributedApplicationBuilder builder,
    //    Func<ResourceGroupResource, IAsyncEnumerable<TResource>> getResources,
    //    Func<TResource, IDictionary<string, string>> getTags)
    //    where TResource : ArmResource
    //{
    //    builder.Services.AddSingleton<IAzureResourceEnumerator>(new AzureResourceEnumerator<TResource>(getResources, getTags));
    //    return builder;
    //}
}

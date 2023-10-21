// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

public static class AzureProvisionerExtensions
{
    /// <summary>
    /// Adds support for generating azure resources dynamically during application startup.
    /// The application must configure the appropriate subscription, location.
    /// </summary>
    public static IDistributedApplicationBuilder AddAzureProvisioning(this IDistributedApplicationBuilder builder)
    {
        builder.Services.AddLifecycleHook<AzureProvisioner>();
        builder.AddAzureProvisioner<AzureKeyVaultResource, KeyVaultProvisoner>();
        builder.AddAzureProvisioner<AzureStorageResource, StorageProvisioner>();
        builder.AddAzureProvisioner<AzureServiceBusResource, ServiceBusProvisioner>();
        return builder;
    }

    internal static IDistributedApplicationBuilder AddAzureProvisioner<TResource, TProvisioner>(this IDistributedApplicationBuilder builder)
        where TResource : IAzureResource
        where TProvisioner : AzureResourceProvisioner<TResource>
    {
        // This lets us avoid using open generics in the caller, we can use keyed lookup instead
        builder.Services.AddKeyedSingleton<IAzuresourceProvisioner, TProvisioner>(typeof(TResource));
        return builder;
    }
}

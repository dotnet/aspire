// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Lifecycle;
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
        builder.Services.TryAddLifecycleHook<AzureResourcePreparer>();
        builder.Services.TryAddLifecycleHook<AzureProvisioner>();

        // Attempt to read azure configuration from configuration
        builder.Services.AddOptions<AzureProvisionerOptions>()
            .BindConfiguration("Azure")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton<TokenCredentialHolder>();

        builder.AddAzureProvisioner<AzureBicepResource, BicepProvisioner>();

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
}

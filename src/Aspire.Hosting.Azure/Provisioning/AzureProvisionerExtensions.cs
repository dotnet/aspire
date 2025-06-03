// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Provisioning.Internal;
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
        // Always add the Azure environment, even if the user doesn't explicitly add it.
#pragma warning disable ASPIREAZURE001
        builder.AddAzureEnvironment();
#pragma warning restore ASPIREAZURE001

        builder.Services.TryAddLifecycleHook<AzureResourcePreparer>();
        builder.Services.TryAddLifecycleHook<AzureProvisioner>();

        // Attempt to read azure configuration from configuration
        builder.Services.AddOptions<AzureProvisionerOptions>()
            .BindConfiguration("Azure")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton<TokenCredentialHolder>();

        // Register internal services for dependency injection and testability
        builder.Services.AddSingleton<IProvisioningContextProvider, DefaultProvisioningContextProvider>();
        builder.Services.AddSingleton<IBicepCliInvoker, DefaultBicepCliInvoker>();
        builder.Services.AddSingleton<IUserSecretsManager, DefaultUserSecretsManager>();

        return builder;
    }
}

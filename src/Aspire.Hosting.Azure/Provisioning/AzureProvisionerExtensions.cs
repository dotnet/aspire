// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        builder.Services.TryAddSingleton<ITokenCredentialProvider, DefaultTokenCredentialProvider>();

        // Register BicepProvisioner via interface
        builder.Services.TryAddSingleton<IBicepProvisioner, BicepProvisioner>();

        // Register the new internal services for testability
        builder.Services.TryAddSingleton<IArmClientProvider, DefaultArmClientProvider>();
        builder.Services.TryAddSingleton<ISecretClientProvider, DefaultSecretClientProvider>();
        builder.Services.TryAddSingleton<IBicepCompiler, BicepCliCompiler>();
        builder.Services.TryAddSingleton<IUserSecretsManager, DefaultUserSecretsManager>();
        builder.Services.TryAddSingleton<IUserPrincipalProvider, DefaultUserPrincipalProvider>();
        if (builder.ExecutionContext.IsPublishMode)
        {
            builder.Services.AddSingleton<IProvisioningContextProvider, PublishModeProvisioningContextProvider>();
        }
        else
        {
            builder.Services.AddSingleton<IProvisioningContextProvider, RunModeProvisioningContextProvider>();
        }
        builder.Services.TryAddSingleton<IProcessRunner, DefaultProcessRunner>();

        return builder;
    }
}

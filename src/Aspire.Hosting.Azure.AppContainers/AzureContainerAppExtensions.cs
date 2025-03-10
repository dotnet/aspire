// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for customizing Azure Container App definitions for projects.
/// </summary>
public static class AzureContainerAppExtensions
{
    /// <summary>
    /// Adds the necessary infrastructure for Azure Container Apps to the distributed application builder.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    public static IDistributedApplicationBuilder AddAzureContainerAppsInfrastructure(this IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // ensure AzureProvisioning is added first so the AzureResourcePreparer lifecycle hook runs before AzureContainerAppsInfrastructure
        builder.AddAzureProvisioning();

        // AzureContainerAppsInfrastructure will handle adding role assignments,
        // so Azure resources don't need to add the default role assignments themselves
        builder.Services.Configure<AzureProvisioningOptions>(o => o.SupportsTargetedRoleAssignments = true);

        builder.Services.TryAddLifecycleHook<AzureContainerAppsInfrastructure>();

        return builder;
    }
}

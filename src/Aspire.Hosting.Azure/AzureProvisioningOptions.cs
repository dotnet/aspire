// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Options to configure the behavior of Azure resources.
/// </summary>
/// <remarks>
/// These options can be configured using https://learn.microsoft.com/dotnet/core/extensions/options#use-di-services-to-configure-options
/// to customize how Azure resources are provisioned.
/// </remarks>
public sealed class AzureProvisioningOptions
{
    /// <summary>
    /// Gets the <see cref="global::Azure.Provisioning.ProvisioningBuildOptions"/> which contains common settings and
    /// functionality for building Azure resources.
    /// </summary>
    public ProvisioningBuildOptions ProvisioningBuildOptions { get; } = new ProvisioningBuildOptions();

    /// <summary>
    /// Gets or sets a value indicating whether the Azure compute infrastructure supports specific role assignments
    /// from an application to an Azure resource.
    /// </summary>
    /// <remarks>
    /// Setting this to true will disable the Azure resources from generating default role assignments
    /// in their bicep templates.
    /// </remarks>
    public bool SupportsTargetedRoleAssignments { get; set; }
}

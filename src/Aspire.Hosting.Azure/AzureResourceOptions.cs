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
public sealed class AzureResourceOptions
{
    /// <summary>
    /// Gets the <see cref="Azure.Provisioning.ProvisioningContext"/> which contains common settings and
    /// functionality for building Azure resources.
    /// </summary>
    public ProvisioningContext ProvisioningContext { get; } = new ProvisioningContext();
}

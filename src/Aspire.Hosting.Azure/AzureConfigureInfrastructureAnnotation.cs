// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Extensions for working with <see cref="AzureProvisioningResource"/> and related types.
/// </summary>
/// <param name="configureInfrastructure">A callback used to configure the infrastructure resource.</param>
public class AzureConfigureInfrastructureAnnotation(Action<AzureResourceInfrastructure> configureInfrastructure) : IResourceAnnotation
{
    /// <summary>
    /// Callback for configuring the Azure resources.
    /// </summary>
    public Action<AzureResourceInfrastructure> ConfigureInfrastructure { get; } = configureInfrastructure;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.AppContainers;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an annotation for customizing an Azure Container App.
/// </summary>
public sealed class AzureContainerAppCustomizationAnnotation(Action<AzureResourceInfrastructure, ContainerApp> configure)
    : IResourceAnnotation
{
    /// <summary>
    /// Gets the configuration action for customizing the Azure Container App.
    /// </summary>
    public Action<AzureResourceInfrastructure, ContainerApp> Configure { get; } = configure ?? throw new ArgumentNullException(nameof(configure));
}

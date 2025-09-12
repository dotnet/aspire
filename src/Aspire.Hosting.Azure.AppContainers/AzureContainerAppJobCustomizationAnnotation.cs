// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.AppContainers;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an annotation for customizing an Azure Container App Job.
/// </summary>
[Experimental("ASPIREAZURE002", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class AzureContainerAppJobCustomizationAnnotation(Action<AzureResourceInfrastructure, ContainerAppJob> configure)
    : IResourceAnnotation
{
    /// <summary>
    /// Gets the configuration action for customizing the Azure Container App Job.
    /// </summary>
    public Action<AzureResourceInfrastructure, ContainerAppJob> Configure { get; } = configure ?? throw new ArgumentNullException(nameof(configure));
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.AppService;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an annotation for customizing an Azure Web App.
/// </summary>
public sealed class AzureAppServiceWebsiteCustomizationAnnotation(Action<AzureResourceInfrastructure, WebSite> configure)
    : IResourceAnnotation
{
    /// <summary>
    /// Gets the configuration action for customizing the Azure Web App.
    /// </summary>
    public Action<AzureResourceInfrastructure, WebSite> Configure { get; } = configure ?? throw new ArgumentNullException(nameof(configure));
}

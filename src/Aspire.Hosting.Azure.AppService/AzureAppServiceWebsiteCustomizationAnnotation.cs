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

/// <summary>
/// Represents an annotation for unique website name and host name.
/// </summary>
public sealed class AzureAppServiceHostNameAnnotation(string websiteName, string hostName, bool existing)
    : IResourceAnnotation
{
    /// <summary>
    /// Gets the name of the Azure Web App.
    /// </summary>
    public string WebsiteName { get; } = websiteName ?? throw new ArgumentNullException(nameof(websiteName));

    /// <summary>
    /// Gets the host name for the Azure Website.
    /// </summary>
    public string HostName { get; } = hostName ?? throw new ArgumentNullException(nameof(hostName));

    /// <summary>
    /// Gets a value indicating whether the website currently exists.
    /// </summary>
    public bool Existing { get; } = existing;
}

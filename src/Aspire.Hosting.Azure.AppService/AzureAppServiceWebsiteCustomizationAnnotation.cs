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
/// Represents an annotation for customizing an Azure Web App slot.
/// </summary>
/// <param name="configure"></param>
public sealed class AzureAppServiceWebsiteSlotCustomizationAnnotation(Action<AzureResourceInfrastructure, WebSiteSlot> configure)
    : IResourceAnnotation
{
    /// <summary>
    /// Gets the configuration action for customizing the Azure Web App.
    /// </summary>
    public Action<AzureResourceInfrastructure, WebSiteSlot> Configure { get; } = configure ?? throw new ArgumentNullException(nameof(configure));
}

/// <summary>
/// Represents an annotation for the creation of an Azure App Service website and its deployment slot, including
/// configuration customization.
/// </summary>
/// <param name="mainWebSiteExists">true to indicate that the main Azure Web App already exists; otherwise, false to indicate that it should be created.</param>
public sealed class AzureAppServiceWebsiteAndSlotCreationAnnotation(bool mainWebSiteExists = false)
    : IResourceAnnotation
{
    /// <summary>
    /// Property indicating whether the main Azure Web App already exists.
    /// </summary>
    public bool MainWebSiteExists { get; } = mainWebSiteExists;
}

/// <summary>
/// 
/// </summary>
internal sealed class AzureAppServiceEnvironmentContextAnnotation (AzureAppServiceEnvironmentContext context)
    : IResourceAnnotation
{
    /// <summary>
    /// 
    /// </summary>
    public AzureAppServiceEnvironmentContext EnvironmentContext { get; } = context;
}

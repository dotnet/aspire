// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.AppService;
/// <summary>
/// Represents an annotation for customizing an Azure Web App.
/// </summary>
public sealed class AzureAppServiceWebSiteAppInsightsAnnotation()
    : IResourceAnnotation
{
    /// <summary>
    /// Constructor for AzureAppServiceWebSiteAppInsightsAnnotation.
    /// </summary>
    /// <param name="enabled"></param>
    public AzureAppServiceWebSiteAppInsightsAnnotation(bool enabled) : this()
    {
        Enabled = enabled;
    }

    /// <summary>
    /// Gets the configuration action for customizing the Azure Web App.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

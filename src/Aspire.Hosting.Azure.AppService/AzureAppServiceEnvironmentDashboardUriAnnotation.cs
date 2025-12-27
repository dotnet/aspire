// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an annotation for the Azure App Service environment context.
/// </summary>
internal sealed class AzureAppServiceEnvironmentDashboardUriAnnotation(string dashboardUri)
    : IResourceAnnotation
{
    /// <summary>
    /// Get the Azure App Service environment dashboard url.
    /// </summary>
    public string DashboardUri { get; } = dashboardUri;
}

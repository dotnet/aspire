// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Well-known pipeline step names for common deployment operations.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics#{0}")]
public static class WellKnownPipelineSteps
{
    /// <summary>
    /// The step that provisions Azure resources using Bicep templates.
    /// </summary>
    public const string ProvisionBicepResources = nameof(ProvisionBicepResources);

    /// <summary>
    /// The step that provisions Azure Container Apps resources.
    /// </summary>
    public const string ProvisionContainerApps = nameof(ProvisionContainerApps);

    /// <summary>
    /// The step that provisions Azure App Service resources.
    /// </summary>
    public const string ProvisionAppService = nameof(ProvisionAppService);

    /// <summary>
    /// The step that builds container images.
    /// </summary>
    public const string BuildContainerImages = nameof(BuildContainerImages);

    /// <summary>
    /// The step that pushes container images to a registry.
    /// </summary>
    public const string PushContainerImages = nameof(PushContainerImages);
}

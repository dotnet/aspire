// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Kubernetes;

/// <summary>
/// Options which control generation of Kubernetes artifacts.
/// </summary>
public sealed class KubernetesPublisherOptions : PublishingOptions
{
    /// <summary>
    /// Gets or sets the name of the Helm chart to be generated.
    /// </summary>
    public string HelmChartName { get; set; } = "aspire";

    /// <summary>
    /// Gets or sets the version of the Helm chart to be generated.
    /// This property specifies the version number that will be assigned to the Helm chart,
    /// typically following semantic versioning conventions.
    /// </summary>
    public string HelmChartVersion { get; set; } = "0.1.0";

    /// <summary>
    /// Gets or sets the description of the Helm chart being generated.
    /// </summary>
    public string HelmChartDescription { get; set; } = "Aspire Helm Chart";
}

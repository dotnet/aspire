// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a Helm Chart metadata definition with properties corresponding to the chart.yaml specification.
/// </summary>
/// <remarks>
/// This class is used to define and serialize Helm Chart metadata into YAML format.
/// It includes information about the chart's version, dependencies, maintainers, app version, and other key details.
/// </remarks>
[YamlSerializable]
public sealed class HelmChart
{
    /// <summary>
    /// Represents the API version of the Helm chart.
    /// </summary>
    /// <remarks>
    /// The ApiVersion property defines the schema version for the Helm chart.
    /// It is a required field and ensures compatibility between the Helm chart
    /// and the chart engine that processes it.
    /// </remarks>
    [YamlMember(Alias = "apiVersion")]
    public string ApiVersion { get; set; } = "v2";

    /// <summary>
    /// Gets or sets the name of the Helm Chart.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the version of the Helm chart.
    /// Represents the specific version of the Helm chart as defined in the Chart.yaml file.
    /// This property is critical for versioning and managing release updates of the Kubernetes resources.
    /// </summary>
    [YamlMember(Alias = "version")]
    public string Version { get; set; } = null!;

    /// <summary>
    /// Specifies the required Kubernetes version for the Helm Chart.
    /// This property allows you to define the compatibility of the chart
    /// with a specific Kubernetes version or version range.
    /// </summary>
    [YamlMember(Alias = "kubeVersion")]
    public string? KubeVersion { get; set; }

    /// <summary>
    /// Gets or sets the description of the Helm Chart.
    /// This provides a brief summary or details about the chart's purpose and functionality.
    /// </summary>
    [YamlMember(Alias = "description")]
    public string Description { get; set; } = null!;

    /// <summary>
    /// Specifies the type of the Helm Chart. This property is optional and can be used
    /// to define the classification or category of the Helm Chart, such as application, library, or a custom type.
    /// </summary>
    [YamlMember(Alias = "type")]
    public string? Type { get; set; }

    /// <summary>
    /// Represents a collection of keywords associated with the Helm chart,
    /// providing descriptors or tags to categorize or identify the chart.
    /// </summary>
    [YamlMember(Alias = "keywords")]
    public List<string> Keywords { get; set; } = [];

    /// <summary>
    /// Gets or sets the URL of the Helm chart's home page or project website.
    /// </summary>
    /// <remarks>
    /// This property typically contains a reference to the home page or documentation
    /// site for the Helm chart, providing users with additional resources or information
    /// about the chart.
    /// </remarks>
    [YamlMember(Alias = "home")]
    public string? Home { get; set; }

    /// <summary>
    /// Represents a collection of URLs or references pointing to the source repositories or locations
    /// associated with the Helm chart. These sources are useful for understanding or confirming the
    /// origins of the chart, reviewing its code, or obtaining additional related information.
    /// </summary>
    [YamlMember(Alias = "sources")]
    public List<string> Sources { get; set; } = [];

    /// <summary>
    /// Represents the list of dependencies for the Helm chart. Dependencies specify other Helm charts that this chart relies on,
    /// including their configurations, such as name, version, repository, and additional metadata.
    /// </summary>
    [YamlMember(Alias = "dependencies")]
    public List<HelmChartDependency> Dependencies { get; set; } = [];

    /// <summary>
    /// Represents the list of maintainers for the Helm chart.
    /// </summary>
    /// <remarks>
    /// Each maintainer is represented as a <see cref="HelmChartMaintainer"/> object.
    /// Maintainers typically include information such as name, email, and URL.
    /// </remarks>
    [YamlMember(Alias = "maintainers")]
    public List<HelmChartMaintainer> Maintainers { get; set; } = [];

    /// <summary>
    /// Gets or sets the URL pointing to the icon of the Helm chart.
    /// This property is typically used to display a visual representation
    /// of the chart in user interfaces.
    /// </summary>
    [YamlMember(Alias = "icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// Represents the version of the application for which this Helm chart is designed.
    /// This property typically aligns with the application's semantic versioning.
    /// </summary>
    [YamlMember(Alias = "appVersion")]
    public string AppVersion { get; set; } = null!;

    /// <summary>
    /// Indicates whether the Helm chart is deprecated.
    /// When set to true, it specifies that the chart is no longer recommended for use
    /// and may not receive updates or support.
    /// </summary>
    [YamlMember(Alias = "deprecated")]
    public bool? Deprecated { get; set; } = false;

    /// <summary>
    /// Represents a collection of annotations associated with the Helm chart.
    /// These annotations provide metadata or additional information in the form of key-value pairs.
    /// </summary>
    [YamlMember(Alias = "annotations")]
    public Dictionary<string, string> Annotations { get; set; } = [];
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a single dependency in a Helm Chart's dependencies section.
/// </summary>
/// <remarks>
/// This class is used to define and serialize Helm Chart dependencies into YAML format.
/// It includes properties such as name, version, and repository information of the dependency,
/// as well as optional conditions, tags, imported values, and aliases.
/// </remarks>
[YamlSerializable]
public sealed class HelmChartDependency
{
    /// <summary>
    /// Gets or sets the name of the Helm chart dependency.
    /// This property specifies the unique name identifying the Helm chart.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the version of the Helm chart dependency.
    /// This property specifies the version of the Helm chart to be used
    /// in the deployment process, ensuring compatibility and correctness.
    /// </summary>
    [YamlMember(Alias = "version")]
    public string Version { get; set; } = null!;

    /// <summary>
    /// Gets or sets the repository URL or location where the Helm chart dependency is located.
    /// </summary>
    [YamlMember(Alias = "repository")]
    public string Repository { get; set; } = null!;

    /// <summary>
    /// Gets or sets the condition associated with the Helm chart dependency.
    /// The condition is used to control whether this dependency is enabled or disabled
    /// based on specific criteria or flags defined in the Helm values.
    /// </summary>
    [YamlMember(Alias = "condition")]
    public string Condition { get; set; } = null!;

    /// <summary>
    /// Gets or sets a list of tags associated with the Helm chart dependency.
    /// Tags are used to manage and conditionally enable dependencies in a Helm chart.
    /// </summary>
    [YamlMember(Alias = "tags")]
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Represents the list of values to be imported into the Helm chart dependency.
    /// These values are used to override or supplement configuration settings within the chart.
    /// </summary>
    [YamlMember(Alias = "import-values")]
    public List<string> ImportValues { get; set; } = [];

    /// <summary>
    /// Gets or sets the alias for the Helm chart dependency.
    /// The alias is an optional identifier that can be used to reference or override the default name of the dependency.
    /// </summary>
    [YamlMember(Alias = "alias")]
    public string Alias { get; set; } = null!;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a maintainer of a Helm Chart as specified in the chart.yaml file.
/// </summary>
/// <remarks>
/// This class holds metadata about a maintainer of a Helm chart, including their name, email, and optional URL.
/// It is typically used in the list of maintainers within the Helm chart metadata structure.
/// </remarks>
[YamlSerializable]
public sealed class HelmChartMaintainer
{
    /// <summary>
    /// Gets or sets the name of the Helm chart maintainer.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the email address of the maintainer for the Helm chart.
    /// </summary>
    [YamlMember(Alias = "email")]
    public string Email { get; set; } = null!;

    /// <summary>
    /// Gets or sets the URL associated with the Helm chart maintainer.
    /// </summary>
    /// <remarks>
    /// This property specifies a web address related to the maintainer, such as a personal website,
    /// documentation, or a project repository URL. It is used for attribution or additional
    /// information about the maintainer.
    /// </remarks>
    [YamlMember(Alias = "url")]
    public string Url { get; set; } = null!;
}

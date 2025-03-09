// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Kubernetes.Helm.Resources;

/// <summary>
/// Represents a Helm chart and its associated metadata, including API version, name, version, and dependencies.
/// Provides functionality to create a new Helm chart object, load chart information from an existing YAML string,
/// and manage dependencies within the chart.
/// </summary>
public sealed class HelmChartInfo : YamlObject
{
    /// <summary>
    /// Represents the structure and metadata of a Helm chart, enabling manipulation
    /// and serialization of its properties in YAML format.
    /// </summary>
    /// <remarks>
    /// This class provides functionality to define or load a Helm chart using its YAML
    /// structure. It includes properties such as API version, name, version, and dependencies,
    /// which are essential components of a Helm chart.
    /// </remarks>
    public HelmChartInfo(string name)
    {
        Add(HelmYamlKeys.ApiVersion, new YamlValue("v2"));
        Add(HelmYamlKeys.Name, new YamlValue(name));
        Add(HelmYamlKeys.Version, new YamlValue("1.0.0"));
        Add(HelmYamlKeys.Dependencies, new YamlArray());
    }

    // Load from an existing `Chart.yaml` content
    /// <summary>
    /// Creates a new <see cref="HelmChartInfo"/> instance by parsing the provided YAML content.
    /// </summary>
    /// <param name="yaml">The YAML content of a Helm Chart, typically from a `Chart.yaml` file.</param>
    /// <returns>A new <see cref="HelmChartInfo"/> instance populated with the data from the YAML content.</returns>
    public static new HelmChartInfo FromYaml(string yaml)
    {
        var obj = YamlObject.FromYaml(yaml);
        var chartName = (obj.Get(HelmYamlKeys.Name) as YamlValue)?.Value.ToString() ?? "aspire-chart";
        var chart = new HelmChartInfo(chartName);

        if (obj.Get(HelmYamlKeys.ApiVersion) is { } apiVer)
        {
            chart.Replace(HelmYamlKeys.ApiVersion, apiVer);
        }

        if (obj.Get(HelmYamlKeys.Version) is { } ver)
        {
            chart.Replace(HelmYamlKeys.Version, ver);
        }

        if (obj.Get(HelmYamlKeys.Dependencies) is { } deps)
        {
            chart.Replace(HelmYamlKeys.Dependencies, deps);
        }

        return chart;
    }

    /// Adds a dependency to the Helm chart.
    /// <param name="name">
    /// The name of the dependency to be added.
    /// </param>
    /// <param name="version">
    /// The version of the dependency.
    /// </param>
    /// <param name="repo">
    /// The repository where the dependency is located.
    /// </param>
    /// <return>
    /// Returns the current instance of <see cref="HelmChartInfo"/> with the updated dependency information.
    /// </return>
    public HelmChartInfo AddDependency(string name, string version, string repo)
    {
        var depObj = new YamlObject();
        depObj.Add(HelmYamlKeys.Name, new YamlValue(name));
        depObj.Add(HelmYamlKeys.Version, new YamlValue(version));
        depObj.Add("repository", new YamlValue(repo));
        var dependencies = GetOrCreate<YamlArray>(HelmYamlKeys.Dependencies);
        dependencies.Add(depObj);
        return this;
    }
}

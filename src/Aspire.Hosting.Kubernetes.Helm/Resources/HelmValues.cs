// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Kubernetes.Helm.Resources;

/// <summary>
/// Represents a collection of Helm chart values, typically loaded from a `values.yaml` file.
/// This class extends the functionality of <see cref="YamlObject"/> to handle Helm-specific use cases.
/// </summary>
/// <remarks>
/// Helm values are typically key-value pairs that define configuration settings for a Helm chart.
/// These settings can override the default configuration specified in the chart's `values.yaml` file.
/// The class supports loading and manipulating YAML-based settings.
/// </remarks>
/// <seealso cref="YamlObject"/>
public sealed class HelmValues : YamlObject
{
    /// <summary>
    /// Creates a new instance of <see cref="HelmValues"/> by parsing the provided YAML string.
    /// </summary>
    /// <param name="yaml">The YAML string representing the configuration values.</param>
    /// <returns>A <see cref="HelmValues"/> object populated with the parsed YAML data.</returns>
    public static new HelmValues FromYaml(string yaml)
    {
        var obj = YamlObject.FromYaml(yaml);
        var vals = new HelmValues();
        // We'll just do a shallow copy here
        foreach (var key in obj.Properties.Keys)
        {
            var node = obj.Get(key);
            if (node != null)
            {
                vals.Add(key, node);
            }
        }
        return vals;
    }
}

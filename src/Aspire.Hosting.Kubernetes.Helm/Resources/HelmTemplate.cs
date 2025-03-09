// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Kubernetes.Helm.Resources;

/// <summary>
/// Represents a Helm template file, which is a YAML file used to define Kubernetes resources
/// in a Helm chart. This class provides mechanisms for creating and loading templates
/// as well as managing their content.
/// </summary>
/// Represents a Helm template in YAML format and provides methods for creating and managing it.
public sealed class HelmTemplate(string fileName) : YamlObject
{
    /// <summary>
    /// Gets the name of the file associated with the Helm template.
    /// </summary>
    /// <remarks>
    /// This property represents the file name of the Helm template, typically following
    /// a structured naming convention that includes the resource type and resource name
    /// (e.g., "resourceType-resourceName.yaml").
    /// </remarks>
    public string FileName { get; } = fileName;

    /// <summary>
    /// Creates a Helm template object using the provided resource type and name.
    /// The generated file name is a combination of the sanitized resource type
    /// and resource name in the format "<resourceType/>-<resourceName/>.yaml".
    /// </summary>
    /// <param name="resourceType">The type of the Kubernetes resource (e.g., Deployment, Service).</param>
    /// <param name="resourceName">The name of the Kubernetes resource.</param>
    /// <returns>A <see cref="HelmTemplate"/> object with a generated file name.</returns>
    public static HelmTemplate CreateTemplate(string resourceType, string resourceName)
    {
        var sanitizedType = resourceType.ToLowerInvariant();
        var sanitizedName = resourceName.ToLowerInvariant();
        var fileName = $"{sanitizedType}-{sanitizedName}.yaml";
        return new HelmTemplate(fileName);
    }

    // Load from a single template file's YAML
    /// <summary>
    /// Creates a new <see cref="HelmTemplate"/> instance from the specified YAML content.
    /// </summary>
    /// <param name="fileName">The name of the file associated with the YAML content.</param>
    /// <param name="yaml">The YAML string used to create the Helm template.</param>
    /// <returns>A <see cref="HelmTemplate"/> instance representing the data from the provided YAML content.</returns>
    public static HelmTemplate FromYaml(string fileName, string yaml)
    {
        var obj = YamlObject.FromYaml(yaml);
        var template = new HelmTemplate(fileName);
        // copy content from obj
        foreach (var key in obj.Properties.Keys)
        {
            var node = obj.Get(key);
            if (node != null)
            {
                template.Add(key, node);
            }
        }
        return template;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Kubernetes.Kustomize.Resources;

/// <summary>
/// Represents a customizable YAML resource file utilized for defining resources
/// in Kubernetes Kustomize configurations.
/// </summary>
/// <remarks>
/// This class encapsulates the creation and manipulation of YAML-based resources,
/// enabling the generation of structured resource files and providing functionality
/// to parse YAML content into a representation suitable for Kustomize.
/// </remarks>
public sealed class KustomResource(string fileName) : YamlObject
{
    /// <summary>
    /// Gets the name of the file associated with the current KustomResource instance.
    /// This property is used to identify and reference the resource file as it appears
    /// in the filesystem or within a kustomization context, typically with a `.yaml` extension.
    /// </summary>
    public string FileName { get; } = fileName;

    /// <summary>
    /// Creates a resource file with a sanitized name based on the specified resource type and name.
    /// </summary>
    /// <param name="resourceType">The type of the resource, which will be sanitized to lowercase format.</param>
    /// <param name="resourceName">The name of the resource, which will also be sanitized to lowercase format.</param>
    /// <returns>A new instance of <see cref="KustomResource"/> representing the YAML file for the resource.</returns>
    public static KustomResource CreateResourceFile(string resourceType, string resourceName)
    {
        var sanitizedType = resourceType.ToLowerInvariant();
        var sanitizedName = resourceName.ToLowerInvariant();
        var fileName = $"{sanitizedType}-{sanitizedName}.yaml";
        return new KustomResource(fileName);
    }

    /// <summary>
    /// Creates a KustomResource instance by parsing the provided YAML content
    /// and associating it with a specified file name.
    /// </summary>
    /// <param name="fileName">The file name to associate with the KustomResource object.</param>
    /// <param name="yaml">The YAML content to parse and convert into a KustomResource.</param>
    /// <returns>A new instance of the KustomResource with the parsed properties from the YAML content.</returns>
    public static KustomResource FromYaml(string fileName, string yaml)
    {
        var obj = YamlObject.FromYaml(yaml);
        var kr = new KustomResource(fileName);
        // copy everything from obj
        foreach (var k in obj.Properties.Keys)
        {
            var node = obj.Get(k);
            if (node != null)
            {
                kr.Add(k, node);
            }
        }
        return kr;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Kubernetes.Kustomize.Resources;

/// <summary>
/// Represents a Kustomization file that is used for configuring and managing Kubernetes resources
/// in the context of Kustomize.
/// </summary>
/// <remarks>
/// This class provides features for managing Kustomize components such as resources, patches,
/// and configuration maps. It extends the YamlObject class to facilitate handling YAML-based content.
/// </remarks>
public sealed class KustomizationFile : YamlObject
{
    /// <summary>
    /// Represents a Kustomization File in Kubernetes Kustomize, used for managing Kubernetes resource configurations.
    /// Inherits from the <see cref="YamlObject"/> class and provides methods to manipulate and parse YAML configurations.
    /// </summary>
    public KustomizationFile()
    {
        Add(KustomizeYamlKeys.ApiVersion, new YamlValue("kustomize.config.k8s.io/v1beta1"));
        Add(KustomizeYamlKeys.Kind, new YamlValue("Kustomization"));
        Add(KustomizeYamlKeys.Resources, new YamlArray());
    }

    /// <summary>
    /// Parses a YAML string and converts it into an instance of the KustomizationFile class.
    /// </summary>
    /// <param name="yaml">The YAML string to be parsed.</param>
    /// <returns>A KustomizationFile instance populated with the data parsed from the input YAML string.</returns>
    public static new KustomizationFile FromYaml(string yaml)
    {
        var obj = YamlObject.FromYaml(yaml);
        var kustom = new KustomizationFile();
        if (obj.Get(KustomizeYamlKeys.Resources) is { } r)
        {
            kustom.Replace(KustomizeYamlKeys.Resources, r);
        }

        if (obj.Get(KustomizeYamlKeys.PatchesStrategicMerge) is { } p)
        {
            kustom.Replace(KustomizeYamlKeys.PatchesStrategicMerge, p);
        }

        if (obj.Get(KustomizeYamlKeys.ConfigMapGenerator) is { } c)
        {
            kustom.Replace(KustomizeYamlKeys.ConfigMapGenerator, c);
        }

        return kustom;
    }

    // Add a resource reference (the path to a separate YAML file).
    /// <summary>
    /// Adds a resource reference by specifying the path to a separate YAML file.
    /// The resource path will be added to the 'resources' key in the kustomization YAML structure.
    /// </summary>
    /// <param name="resourcePath">The path to the resource YAML file to be added.</param>
    /// <returns>The current instance of <see cref="KustomizationFile"/> to allow method chaining.</returns>
    public KustomizationFile AddResource(string resourcePath)
    {
        var resources = GetOrCreate<YamlArray>(KustomizeYamlKeys.Resources);
        resources.Add(new YamlValue(resourcePath));
        return this;
    }

    // Add a patch reference
    /// <summary>
    /// Adds a patch reference to the kustomization file.
    /// </summary>
    /// <param name="patchPath">The file path to the patch to be added.</param>
    /// <returns>The updated <see cref="KustomizationFile"/> instance.</returns>
    public KustomizationFile AddPatch(string patchPath)
    {
        var patches = GetOrCreate<YamlArray>(KustomizeYamlKeys.PatchesStrategicMerge);
        patches.Add(new YamlValue(patchPath));
        return this;
    }

    // Add a configMap generator entry
    /// Adds a config map generator entry to the Kustomization file.
    /// <param name="name">
    /// The name of the ConfigMap to be added.
    /// </param>
    /// <param name="data">
    /// A dictionary containing the key-value pairs to be included as literals in the ConfigMap.
    /// </param>
    /// <returns>
    /// The updated KustomizationFile instance with the added ConfigMap.
    /// </returns>
    public KustomizationFile AddConfigMap(string name, Dictionary<string, string> data)
    {
        var mapObj = new YamlObject();
        mapObj.Add("name", new YamlValue(name));
        var literalsArr = new YamlArray();
        foreach (var (k, v) in data)
        {
            literalsArr.Add(new YamlValue($"{k}={v}"));
        }
        mapObj.Add("literals", literalsArr);
        var resources = GetOrCreate<YamlArray>(KustomizeYamlKeys.ConfigMapGenerator);
        resources.Add(mapObj);
        return this;
    }
}

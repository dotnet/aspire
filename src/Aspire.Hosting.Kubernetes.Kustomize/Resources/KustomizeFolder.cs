// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Kubernetes.Kustomize.Resources;

/// <summary>
/// Represents a folder containing Kustomize configuration data and resources.
/// </summary>
public sealed class KustomizeFolder
{
    /// <summary>
    /// Represents the kustomization configuration associated with a kustomization folder.
    /// This property provides access to the core kustomization file, which defines and manages
    /// resources, patches, and configuration for Kubernetes deployments. It ensures integration
    /// with resource files and allows manipulating the kustomization data explicitly.
    /// </summary>
    public KustomizationFile Kustomization { get; }

    /// <summary>
    /// Gets the collection of KustomResource instances associated with the KustomizationFolder.
    /// </summary>
    /// <remarks>
    /// This property holds the list of Kubernetes resource definitions to be included in the
    /// Kustomize configuration. Each resource in the list is represented as a KustomResource,
    /// providing methods and properties for YAML manipulation. The collection is automatically
    /// populated when resources are added or loaded from a directory.
    /// </remarks>
    public List<KustomResource> Resources { get; } = [];

    /// <summary>
    /// Represents a folder containing a kustomization definition and associated Kubernetes resources.
    /// </summary>
    public KustomizeFolder()
    {
        Kustomization = new KustomizationFile();
    }

    /// <summary>
    /// Adds a KustomResource to the collection of resources and updates the KustomizationFile
    /// to include a reference to the resource file.
    /// </summary>
    /// <param name="resource">
    /// The KustomResource object representing a Kubernetes resource to be added to the folder
    /// and referenced in the KustomizationFile.
    /// </param>
    public void AddResourceFile(KustomResource resource)
    {
        Resources.Add(resource);
        // ensure the kustomization references the file
        Kustomization.AddResource(resource.FileName);
    }

    /// <summary>
    /// Writes the KustomizeFolder's contents, including the kustomization file and contained resources,
    /// to the specified directory on the filesystem.
    /// </summary>
    /// <param name="folderPath">The path to the directory where the folder's contents should be written.</param>
    public void WriteToDirectory(string folderPath)
    {
        Directory.CreateDirectory(folderPath);
        // write kustomization.yaml
        var kustomPath = Path.Combine(folderPath, "kustomization.yaml");
        File.WriteAllText(kustomPath, Kustomization.ToYamlString());

        // write resource files
        foreach (var res in Resources)
        {
            var path = Path.Combine(folderPath, res.FileName);
            File.WriteAllText(path, res.ToYamlString());
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="KustomizeFolder"/> by reading and parsing data from the specified directory.
    /// </summary>
    /// <param name="folderPath">The path of the directory containing the "kustomization.yaml" file.</param>
    /// <returns>A new <see cref="KustomizeFolder"/> initialized with data from the specified directory.</returns>
    public static KustomizeFolder FromDirectory(string folderPath)
    {
        var kustomFile = Path.Combine(folderPath, "kustomization.yaml");
        var kustomContent = File.ReadAllText(kustomFile);
        var kustomObj = KustomizationFile.FromYaml(kustomContent);

        var folder = new KustomizeFolder();

        // copy existing top-level fields
        folder.Kustomization.Replace(KustomizeYamlKeys.Resources, kustomObj.Get(KustomizeYamlKeys.Resources) ?? new YamlArray());
        folder.Kustomization.Replace(KustomizeYamlKeys.PatchesStrategicMerge, kustomObj.Get(KustomizeYamlKeys.PatchesStrategicMerge) ?? new YamlArray());
        folder.Kustomization.Replace(KustomizeYamlKeys.ConfigMapGenerator, kustomObj.Get(KustomizeYamlKeys.ConfigMapGenerator) ?? new YamlArray());

        // load resource references
        if (folder.Kustomization.Get(KustomizeYamlKeys.Resources) is YamlArray resourcesList)
        {
            foreach (var node in resourcesList.Items)
            {
                switch (node)
                {
                    case YamlValue pathVal:
                    {
                        var filePath = pathVal.Value.ToString();
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            var fullPath = Path.Combine(folderPath, filePath);
                            if (File.Exists(fullPath))
                            {
                                var yaml = File.ReadAllText(fullPath);
                                var resObj = KustomResource.FromYaml(filePath, yaml);
                                folder.Resources.Add(resObj);
                            }
                        }

                        break;
                    }
                }
            }
        }

        return folder;
    }
}

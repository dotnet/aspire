// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Kubernetes.Generators;

internal sealed class KustomizeGenerator(
    DistributedApplicationModel model,
    DistributedApplicationExecutionContext executionContext,
    string outputDirectory,
    ILogger logger,
    CancellationToken cancellationToken) : BaseKubernetesOutputGenerator(model, executionContext, outputDirectory, logger, cancellationToken)
{
    public override async Task WriteManifests()
    {
        Logger.StartGeneratingKustomize();

        ArgumentNullException.ThrowIfNull(Model);

        if (string.IsNullOrEmpty(OutputDirectory))
        {
            throw new ArgumentNullException(nameof(outputDirectory));
        }

        Directory.CreateDirectory(OutputDirectory);

        // We'll collect paths for each resource, to list them in kustomization.yaml
        var resourceFiles = new List<string>();

        // Get all the manifests
        var manifests = CreateManifestsFromModel();

        // Write each manifest to a file
        await InternalWriteManifests(manifests, OutputDirectory, resourceFiles).ConfigureAwait(false);

        // Create the kustomization.yaml object
        // For now, we just have resources: ...
        // Optionally, we can specify namePrefix, commonLabels, etc.
        var kustomization = new Dictionary<string, object>
        {
            [KubernetesPublisherManifestKeys.ApiVersion] = "kustomize.config.k8s.io/v1beta1",
            [KubernetesPublisherManifestKeys.Kind] = "Kustomization",
            [KubernetesPublisherManifestKeys.Resources] = resourceFiles,
        };

        // For example, we could add a name prefix
        // kustomization["namePrefix"] = "demo-";

        // Or add common labels
        // kustomization["commonLabels"] = new Dictionary<string, string>
        // {
        //     ["app.kubernetes.io/name"] = "aspire-sample-app"
        // };

        // Serialize and write kustomization.yaml
        var kustomizationYaml = YamlSerializer.Serialize(kustomization);
        await File.WriteAllTextAsync(Path.Combine(OutputDirectory, "kustomization.yaml"), kustomizationYaml, CancellationToken).ConfigureAwait(false);

        Logger.FinishGeneratingKustomize(OutputDirectory);
    }

    private async Task InternalWriteManifests(IEnumerable<IKubernetesObject<V1ObjectMeta>> models, string outputDirectory, List<string> resourceFiles)
    {
        foreach (var resource in models)
        {
            // Derive a file name such as: "deployment-my-app.yaml"
            var kind = resource.Kind ?? "UnknownKind";
            var name = resource.Metadata?.Name ?? "noname";
            var fileName = $"{kind.ToLowerInvariant()}-{name}.yaml";
            var filePath = Path.Combine(outputDirectory, fileName);

            // Serialize the resource to YAML
            // We *could* or may have to do a manual transform to plain dictionaries,
            // The kubernetes-csharp client models are typically JSON-annotated,
            // which YamlDotNet can often interpret or we can do a quick conversion.
            // If direct serialization fails, we can map the resource to a dictionary
            // as in the Helm generator, but without placeholders.
            string yaml;
            try
            {
                yaml = YamlSerializer.Serialize(resource);
            }
            catch
            {
                // As an alternative approach, we can convert
                // using the client library's built-in JSON serializer and
                // then re-deserialize into a Dictionary
                var json = KubernetesJson.Serialize(resource);
                var dictionary = KubernetesJson.Deserialize<Dictionary<object, object>>(json);
                yaml = YamlSerializer.Serialize(dictionary);
            }

            // Write the file
            await File.WriteAllTextAsync(filePath, yaml, CancellationToken).ConfigureAwait(false);

            // Keep track of it for kustomization.yaml
            resourceFiles.Add(fileName);
        }
    }
}


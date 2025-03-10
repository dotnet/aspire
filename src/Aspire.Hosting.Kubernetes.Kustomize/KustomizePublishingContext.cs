// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Kubernetes.Kustomize.Resources;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Yaml;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Kubernetes.Kustomize;

/// <summary>
/// Represents the context for publishing Kubernetes manifests using Kustomize.
/// </summary>
/// <remarks>
/// This class is responsible for generating and writing output files based on
/// the <see cref="DistributedApplicationModel"/>. It uses provided configurations
/// and logs operation details during the publishing process.
/// </remarks>
internal sealed class KustomizePublishingContext(
    DistributedApplicationExecutionContext executionContext,
    KustomizePublisherOptions publisherOptions,
    ILogger logger,
    CancellationToken cancellationToken = default)
{
    public readonly PortAllocator PortAllocator = new();
    // private ILogger Logger => logger;
    // private KustomizePublisherOptions Options => publisherOptions;

    internal async Task WriteModel(DistributedApplicationModel model)
    {
        if (executionContext.IsRunMode)
        {
            return;
        }

        logger.StartGeneratingKustomize();

        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(publisherOptions.OutputPath);

        if (model.Resources.Count == 0)
        {
            logger.WriteMessage("No resources found in the model.");
            return;
        }

        var outputFile = await WriteKustomizeOutput(model).ConfigureAwait(false);

        logger.FinishGeneratingKustomize(outputFile);
    }

    private async Task<string> WriteKustomizeOutput(DistributedApplicationModel model)
    {
        var kustomizationFile = new KustomizationFile();

        Directory.CreateDirectory(publisherOptions.OutputPath!);

        foreach (var r in model.Resources)
        {
            if (r.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var lastAnnotation) &&
                lastAnnotation == ManifestPublishingCallbackAnnotation.Ignore)
            {
                continue;
            }

            if (!r.IsContainer() && r is not ProjectResource)
            {
                continue;
            }

            var kustomizeServiceContext = new KustomizeServiceContext(r, this);
            var template = kustomizeServiceContext.BuildKustomTemplate();
            var templateOutputFile = Path.Combine(publisherOptions.OutputPath!, template.FileName);
            var templateContents = template.ToYamlString();
            await File.WriteAllTextAsync(templateOutputFile, templateContents, cancellationToken).ConfigureAwait(false);

            kustomizationFile.AddResource(template.FileName);
        }

        var composeOutput = kustomizationFile.ToYamlString();
        var outputFile = Path.Combine(publisherOptions.OutputPath!, "kustomization.yaml");
        await File.WriteAllTextAsync(outputFile, composeOutput, cancellationToken).ConfigureAwait(false);
        return outputFile;
    }

    private sealed class KustomizeServiceContext(IResource resource, KustomizePublishingContext kustomizePublishingContext)
    {
        public KustomResource BuildKustomTemplate()
        {
            _ = kustomizePublishingContext;

            var kustomTemplate = KustomResource.CreateResourceFile("deployment", resource.Name.ToLowerInvariant());
            kustomTemplate.Add("apiVersion", new YamlValue("apps/v1"));
            kustomTemplate.Add("kind", new YamlValue("Deployment"));

            // todo: Handle

            return kustomTemplate;
        }
    }
}

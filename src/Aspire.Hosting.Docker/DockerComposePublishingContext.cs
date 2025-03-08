// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Contextual information used for manifest publishing during this execution of the AppHost as docker compose output format.
/// </summary>
/// <param name="executionContext">Global contextual information for this invocation of the AppHost.</param>
/// <param name="outputPath">Output path for assets generated via this invocation of the AppHost.</param>
/// <param name="logger">The current publisher logger instance.</param>
/// <param name="cancellationToken">Cancellation token for this operation.</param>
internal sealed class DockerComposePublishingContext(DistributedApplicationExecutionContext executionContext, string outputPath, ILogger logger, CancellationToken cancellationToken = default)
{
    private readonly ISerializer _yamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    internal async Task WriteModel(DistributedApplicationModel model)
    {
        logger.StartGeneratingDockerCompose();

        ArgumentNullException.ThrowIfNull(model);

        Directory.CreateDirectory(outputPath);

        // We'll store each Deployment as one Docker Compose "service"
        // We'll also gather all Services, ConfigMaps, and Secrets for potential merging.
        var resources = GroupResources(model);

        if (resources.Invalid.Count != 0)
        {
            throw new DistributedApplicationException("Unsupported resource types: " + string.Join(", ", resources.Invalid.Select(r => r.Kind)));
        }

        // Compose file structure: services
        var composeDoc = new Dictionary<string, object>
        {
            // version is now deprecated, and we won't include it by default...
            [DockerComposePublisherManifestKeys.Services] = new Dictionary<string, object>(),
        };

        if (composeDoc[DockerComposePublisherManifestKeys.Services] is not Dictionary<string, object> servicesSection)
        {
            throw new DistributedApplicationException("Failed to create services section in Docker Compose file.");
        }

        // We'll attempt to merge each Deployment with a matching K8s Service/ConfigMap/Secret as best we can
        ProcessDeployments(servicesSection, resources);

        // Serialize the dictionary as YAML
        var yamlString = _yamlSerializer.Serialize(composeDoc);
        await File.WriteAllTextAsync(Path.Combine(outputPath, "docker-compose.yaml"), yamlString, cancellationToken).ConfigureAwait(false);

        logger.FinishGeneratingDockerCompose(outputPath);
    }

    private FilteredResources GroupResources(DistributedApplicationModel model)
    {
        // TODO: This is a placeholder for now, we'll need to implement this method

        _ = model;
        _ = executionContext;

        return new FilteredResources([], [], [], [], []);
    }

    private static void ProcessDeployments(
        Dictionary<string, object> servicesSection,
        FilteredResources resources)
    {
        foreach (var deployment in resources.Deployments)
        {
            var svcName = deployment.Metadata?.Name ?? throw new DistributedApplicationException("Deployment has no name and cannot be published.");
            var serviceDefinition = new Dictionary<string, object>();

            var mainContainer = deployment.Spec?.Template?.Spec?.Containers?.FirstOrDefault() ?? throw new DistributedApplicationException("Deployment has no containers and cannot be published.");

            serviceDefinition[DockerComposePublisherManifestKeys.Image] = mainContainer.Image ?? throw new DistributedApplicationException("Deployment has no main image and cannot be published.");

            serviceDefinition[DockerComposePublisherManifestKeys.Restart] = "unless-stopped";

            SetupContainerEnvironment(mainContainer, resources.ConfigMaps, resources.Secrets, deployment, serviceDefinition);
            SetupContainerPorts(mainContainer, resources.Services, deployment, serviceDefinition);

            servicesSection[svcName] = serviceDefinition;
        }
    }

    private static void SetupContainerEnvironment(
        V1Container container,
        List<V1ConfigMap> configMaps,
        List<V1Secret> secrets,
        V1Deployment deployment,
        Dictionary<string, object> serviceDefinition)
    {
        var envDict = new Dictionary<string, string>();

        if (container.Env != null)
        {
            foreach (var envVar in container.Env)
            {
                if (!string.IsNullOrEmpty(envVar.Value))
                {
                    envDict[envVar.Name] = envVar.Value;
                    continue;
                }

                if (envVar.ValueFrom?.ConfigMapKeyRef != null)
                {
                    var cmRef = envVar.ValueFrom.ConfigMapKeyRef;
                    // We'll place a placeholder referencing env from that configmap:
                    envDict[envVar.Name] = $"${cmRef.Name}_{cmRef.Key}".ToUpper();
                    continue;
                }

                if (envVar.ValueFrom?.SecretKeyRef == null)
                {
                    continue;
                }

                var secretRef = envVar.ValueFrom.SecretKeyRef;
                envDict[envVar.Name] = $"${secretRef.Name}_{secretRef.Key}".ToUpper();
            }
        }

        // Merge in ConfigMaps if we can find them matching with same deployment name.
        var matchingCm = configMaps.FirstOrDefault(cm => cm.Metadata?.Name == deployment.Metadata?.Name);
        if (matchingCm?.Data != null)
        {
            foreach (var kvp in matchingCm.Data)
            {
                envDict[kvp.Key] = kvp.Value;
            }
        }

        // Merge in Secrets if we can find them matching with same deployment name.
        var matchingSecret = secrets.FirstOrDefault(sec => sec.Metadata?.Name == deployment.Metadata?.Name);
        if (matchingSecret is {StringData: not null})
        {
            foreach (var kvp in matchingSecret.StringData)
            {
                envDict[kvp.Key] = kvp.Value;
            }
        }

        if (envDict.Count != 0)
        {
            serviceDefinition[DockerComposePublisherManifestKeys.Environment] = envDict;
        }
    }

    private static void SetupContainerPorts(
        V1Container container,
        List<V1Service> services,
        V1Deployment deployment,
        Dictionary<string, object> serviceDefinition)
    {
        var portsList = new List<string>();

        if (container.Ports != null)
        {
            portsList.AddRange(container.Ports.Select(p => $"{p.ContainerPort}:{p.ContainerPort}"));
        }

        // Check for matching K8s Service by label selector
        // Typically, a Service selects pods by labels. We'll look for a Service that
        // references the same label as the Deployment's selector.
        var matchLabels = deployment.Spec?.Selector?.MatchLabels ?? new Dictionary<string, string>();
        var matchingService = services.FirstOrDefault(s =>
        {
            // If s.Spec.Selector matches the deployment's labels
            if (s.Spec?.Selector == null)
            {
                return false;
            }

            // For simplicity, we check if ALL key-value pairs in s.Spec.Selector
            // are in the Deployment's matchLabels
            return s.Spec.Selector.All(kvp =>
                matchLabels.ContainsKey(kvp.Key) &&
                matchLabels[kvp.Key] == kvp.Value
            );
        });

        // Merge the service's ports
        if (matchingService?.Spec?.Ports != null)
        {
            foreach (var p in matchingService.Spec.Ports)
            {
                // Compose "ports" in the form "hostPort:containerPort"
                // We can guess containerPort from .targetPort or .port
                // Probably needs a bit of work...
                if (int.TryParse(p.TargetPort?.Value, out var containerPort))
                {
                    portsList.Add($"{p.Port}:{containerPort}");
                }
            }
        }

        // If we have any ports, expose them...
        if (portsList.Count != 0)
        {
            // Remove duplicates
            portsList = [.. portsList.Distinct()];
            serviceDefinition[DockerComposePublisherManifestKeys.Ports] = portsList;
        }
    }

    private sealed record FilteredResources(
        List<V1Deployment> Deployments,
        List<V1ConfigMap> ConfigMaps,
        List<V1Secret> Secrets,
        List<V1Service> Services,
        List<IKubernetesObject<V1ObjectMeta>> Invalid);
}

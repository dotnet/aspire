// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Kubernetes.Generators;

internal sealed class HelmOutputGenerator(
    DistributedApplicationModel model,
    DistributedApplicationExecutionContext executionContext,
    string outputDirectory,
    ILogger logger,
    CancellationToken cancellationToken) : BaseKubernetesOutputGenerator(model, executionContext, outputDirectory, logger, cancellationToken)
{
    private const string ReleaseNamePlaceholder = "{{ .Release.Name }}";

    // Instead of referencing a single .Values.image.* for every container,
    // we will build a .Values.images.<containerName> structure for each container.
    private static string ImageNamePlaceholder(string containerName) =>
        $"{{{{ .Values.images.{containerName}.repository }}}}:{{{{ .Values.images.{containerName}.tag }}}}";

    private static string ImagePullPolicyPlaceholder(string containerName) =>
        $"{{{{ .Values.images.{containerName}.pullPolicy }}}}";

    // We'll do a similar approach for services.
    // For example, if the service is named 'my-service', we'll reference:
    //   {{ .Values.services.my-service.type }}
    //   {{ .Values.services.my-service.port }}
    private static string ServiceTypePlaceholder(string serviceName) =>
        $"{{{{ .Values.services.{serviceName}.type }}}}";

    private static string ServicePortPlaceholder(string serviceName) =>
        $"{{{{ .Values.services.{serviceName}.port }}}}";

    // For secrets, we'll store them under .Values.secrets.<secretName>.<key>.
    private static string SecretValuePlaceholder(string secretName, string key) =>
        $"{{{{ .Values.secrets.{secretName}.{key} }}}}";

    public override async Task WriteManifests()
    {
        Logger.StartGeneratingHelmChart();

        ArgumentNullException.ThrowIfNull(Model);

        if (string.IsNullOrEmpty(OutputDirectory))
        {
            throw new ArgumentNullException(nameof(outputDirectory));
        }

        Directory.CreateDirectory(OutputDirectory);

        var templatesDir = Path.Combine(OutputDirectory, "templates");
        Directory.CreateDirectory(templatesDir);

        var chartYamlObj = GenerateChartYamlManifest();

        // Prepare a dictionary for values.yaml
        // We'll add fields (images, services, secrets, etc.) as we discover them while generating templates.
        var valuesObj = new Dictionary<string, object>();

        // Get all the manifests
        var manifests = CreateManifestsFromModel();

        // Generate templates for each resource
        await ProcessModels(manifests, valuesObj, templatesDir, CancellationToken).ConfigureAwait(false);

        // Write Chart.yaml
        var chartYaml = YamlSerializer.Serialize(chartYamlObj);
        await File.WriteAllTextAsync(Path.Combine(OutputDirectory, "Chart.yaml"), chartYaml, CancellationToken).ConfigureAwait(false);

        // Write values.yaml (if empty, it will just be empty YAML)
        var valuesYaml = YamlSerializer.Serialize(valuesObj);
        await File.WriteAllTextAsync(Path.Combine(OutputDirectory, "values.yaml"), valuesYaml, CancellationToken).ConfigureAwait(false);

        // Write .helmignore
        await File.WriteAllTextAsync(Path.Combine(OutputDirectory, ".helmignore"), DefaultHelmIgnore, CancellationToken).ConfigureAwait(false);

        Logger.FinishGeneratingHelmChart(OutputDirectory);
    }

    private async Task ProcessModels(
        IEnumerable<IKubernetesObject<V1ObjectMeta>> models,
        Dictionary<string, object> valuesObj,
        string templatesDir,
        CancellationToken cancellationToken)
    {
        foreach (var resource in models)
        {
            // Build a dictionary that represents the resource's YAML structure
            var doc = resource switch
            {
                V1Deployment deployment => GenerateDeploymentManifest(deployment, valuesObj),
                V1Service svc => GenerateServiceManifest(svc, valuesObj),
                V1ConfigMap configMap => GenerateConfigMapManifest(configMap),
                V1Secret secret => GenerateSecretManifest(secret, valuesObj),
                _ => null,
            };

            if (doc is null)
            {
                continue;
            }

            var name = resource.Metadata?.Name ?? resource.Kind?.ToLower() ?? throw new DistributedApplicationException("Resource name is null, and cannot be published.");
            var kind = resource.Kind ?? throw new DistributedApplicationException("Resource kind is null, and cannot be published.");
            var fileName = $"{name}-{kind.ToLower()}.yaml";
            var filePath = Path.Combine(templatesDir, fileName);
            var yamlString = YamlSerializer.Serialize(doc);
            await File.WriteAllTextAsync(filePath, yamlString, cancellationToken).ConfigureAwait(false);
        }
    }

    private static Dictionary<string, object> GenerateDeploymentManifest(
        V1Deployment deployment,
        Dictionary<string, object> valuesObj)
    {
        var deployName = deployment.Metadata?.Name ?? throw new DistributedApplicationException("Deployment name is null, and cannot be published.");

        // We'll just support single containers per deployment for now...
        var container = deployment.Spec?.Template?.Spec?.Containers.FirstOrDefault() ?? throw new DistributedApplicationException("Containers are null, and cannot be published.");
        var containerName = container.Name ?? throw new DistributedApplicationException("Container name is null, and cannot be published.");

        AssignContainerToImages(valuesObj, container, containerName);

        // Fill in the dictionary for a Deployment
        return new Dictionary<string, object>
        {
            [KubernetesPublisherManifestKeys.ApiVersion] = "apps/v1",
            [KubernetesPublisherManifestKeys.Kind] = "Deployment",
            [KubernetesPublisherManifestKeys.Metadata] = new Dictionary<string, object>
            {
                [KubernetesPublisherManifestKeys.Name] = $"{ReleaseNamePlaceholder}-{deployName}",
                [KubernetesPublisherManifestKeys.Labels] = new Dictionary<string, object>
                {
                    [KubernetesPublisherManifestKeys.App] = $"{ReleaseNamePlaceholder}-{deployName}",
                },
            },
            [KubernetesPublisherManifestKeys.Spec] = new Dictionary<string, object>
            {
                [KubernetesPublisherManifestKeys.Replicas] = 1,
                [KubernetesPublisherManifestKeys.Selector] = new Dictionary<string, object>
                {
                    [KubernetesPublisherManifestKeys.Matchlabels] = new Dictionary<string, object>
                    {
                        [KubernetesPublisherManifestKeys.App] = $"{ReleaseNamePlaceholder}-{deployName}",
                    },
                },
                [KubernetesPublisherManifestKeys.Template] = new Dictionary<string, object>
                {
                    [KubernetesPublisherManifestKeys.Metadata] = new Dictionary<string, object>
                    {
                        [KubernetesPublisherManifestKeys.Labels] = new Dictionary<string, object>
                        {
                            [KubernetesPublisherManifestKeys.App] = $"{ReleaseNamePlaceholder}-{deployName}",
                        },
                    },
                    [KubernetesPublisherManifestKeys.Spec] = new Dictionary<string, object>
                    {
                        [KubernetesPublisherManifestKeys.Containers] = new Dictionary<string, object>
                        {
                            [KubernetesPublisherManifestKeys.Name] = containerName,
                            [KubernetesPublisherManifestKeys.Image] = ImageNamePlaceholder(containerName),
                            [KubernetesPublisherManifestKeys.ImagePullPolicy] = ImagePullPolicyPlaceholder(containerName),
                            [KubernetesPublisherManifestKeys.Ports] = container.Ports?.Select(
                                    p => new Dictionary<string, object>
                                    {
                                        [KubernetesPublisherManifestKeys.ContainerPort] = p.ContainerPort,
                                        [KubernetesPublisherManifestKeys.Port] = p.HostPort ?? p.ContainerPort,
                                        [KubernetesPublisherManifestKeys.Protocol] = p.Protocol ?? throw new DistributedApplicationException("Container port protocol is null, and cannot be published."),
                                        [KubernetesPublisherManifestKeys.Name] = p.Name,
                                    })
                                .Cast<object>()
                                .ToList() ?? [],
                        },
                    },
                },
            },
        };
    }

    private static Dictionary<string, object> GenerateServiceManifest(
        V1Service service,
        Dictionary<string, object> valuesObj)
    {
        var svcName = service.Metadata?.Name ?? "service";

        // Extract service type/port from the actual V1Service for defaults
        var svcType = service.Spec?.Type ?? "ClusterIP";
        var svcPort = 80;
        var targetPort = 80;
        if (service.Spec?.Ports is { Count: > 0 })
        {
            svcPort = service.Spec.Ports[0].Port;
            if (service.Spec.Ports[0].TargetPort != null)
            {
                if (int.TryParse(service.Spec.Ports[0].TargetPort.Value, out var portAsInt))
                {
                    targetPort = portAsInt;
                }
            }
        }

        AssignServiceInValues(valuesObj, svcName, svcType, svcPort);

        return new Dictionary<string, object>
        {
            [KubernetesPublisherManifestKeys.ApiVersion] = "v1",
            [KubernetesPublisherManifestKeys.Kind] = "Service",
            [KubernetesPublisherManifestKeys.Metadata] = new Dictionary<string, object>
            {
                [KubernetesPublisherManifestKeys.Name] = $"{ReleaseNamePlaceholder}-{svcName}",
            },
            [KubernetesPublisherManifestKeys.Spec] = new Dictionary<string, object>
            {
                [KubernetesPublisherManifestKeys.Type] = ServiceTypePlaceholder(svcName),
                [KubernetesPublisherManifestKeys.Ports] = new List<object>()
                {
                    new Dictionary<string, object>
                    {
                        [KubernetesPublisherManifestKeys.Port] = ServicePortPlaceholder(svcName),
                        [KubernetesPublisherManifestKeys.TargetPort] = targetPort,
                    },
                },
                [KubernetesPublisherManifestKeys.Selector] = new Dictionary<string, object>
                {
                    [KubernetesPublisherManifestKeys.App] = $"{ReleaseNamePlaceholder}-{svcName}",
                },
            },
        };
    }

    private static Dictionary<string, object> GenerateConfigMapManifest(V1ConfigMap configMap)
    {
        var cmName = configMap.Metadata?.Name ?? throw new DistributedApplicationException("Config Map Name is null, and cannot be published.");

        return new Dictionary<string, object>
        {
            [KubernetesPublisherManifestKeys.ApiVersion] = "v1",
            [KubernetesPublisherManifestKeys.Kind] = "ConfigMap",
            [KubernetesPublisherManifestKeys.Metadata] = new Dictionary<string, object>
            {
                [KubernetesPublisherManifestKeys.Name] = $"{ReleaseNamePlaceholder}-{cmName}",
            },
            [KubernetesPublisherManifestKeys.Data] = AssignConfigMapData(configMap),
        };
    }

    private static Dictionary<string, object> GenerateSecretManifest(
        V1Secret secret,
        Dictionary<string, object> valuesObj)
    {
        var secName = secret.Metadata?.Name ?? throw new DistributedApplicationException("Secret Name is null, and cannot be published.");

        return new Dictionary<string, object>
        {
            [KubernetesPublisherManifestKeys.ApiVersion] = "v1",
            [KubernetesPublisherManifestKeys.Kind] = "Secret",
            [KubernetesPublisherManifestKeys.Metadata] = new Dictionary<string, object>
            {
                [KubernetesPublisherManifestKeys.Name] = $"{ReleaseNamePlaceholder}-{secName}",
            },
            [KubernetesPublisherManifestKeys.Type] = secret.Type ?? "Opaque",
            [KubernetesPublisherManifestKeys.StringData] = AssignSecretStringData(secret, valuesObj, secName),
        };
    }

    private static Dictionary<string, object> GenerateChartYamlManifest() =>
        new()
        {
            [KubernetesPublisherManifestKeys.ApiVersion] = "v2",
            [KubernetesPublisherManifestKeys.Name] = "aspire", // or derive from somewhere...
            [KubernetesPublisherManifestKeys.Description] = "A Helm chart for Kubernetes resources",
            [KubernetesPublisherManifestKeys.Type] = "application",
            [KubernetesPublisherManifestKeys.Version] = "0.1.0",
            [KubernetesPublisherManifestKeys.AppVersion] = "1.0.0",
        };

    private static string DefaultHelmIgnore =>
        """
        # Files to ignore when packaging the chart
        *.md
        *.txt
        .git/
        *.bak
        """;

    private static void AssignServiceInValues(Dictionary<string, object> valuesObj, string svcName, string svcType, int svcPort)
    {
        // Now ensure .Values.services.<svcName> is populated
        if (!valuesObj.TryGetValue("services", out var servicesObj))
        {
            servicesObj = new Dictionary<string, object>();
            valuesObj["services"] = servicesObj;
        }

        if (servicesObj is not Dictionary<string, object> servicesDict)
        {
            throw new DistributedApplicationException("Expected .Values.services to be a dictionary.");
        }

        // This service's sub-dict
        servicesDict[svcName] = new Dictionary<string, object>
        {
            [KubernetesPublisherManifestKeys.Type] = svcType,
            [KubernetesPublisherManifestKeys.Port] = svcPort,
        };
    }

    private static Dictionary<string, object> AssignConfigMapData(V1ConfigMap configMap)
    {
        var dataSection = new Dictionary<string, object>();

        if (configMap.Data != null)
        {
            foreach (var kvp in configMap.Data)
            {
                dataSection[kvp.Key] = kvp.Value;
            }
        }

        return dataSection;
    }

    private static Dictionary<string, object> AssignSecretStringData(V1Secret secret, Dictionary<string, object> valuesObj, string secName)
    {
        var stringDataDict = new Dictionary<string, object>();

        // Ensure .Values.secrets exists
        if (!valuesObj.TryGetValue(KubernetesPublisherManifestKeys.Secrets, out var secretsObj))
        {
            secretsObj = new Dictionary<string, object>();
            valuesObj[KubernetesPublisherManifestKeys.Secrets] = secretsObj;
        }
        var secretsDict = (Dictionary<string, object>)secretsObj;

        // Create or overwrite this secret's sub-dict in .Values.secrets
        var secretSubDict = new Dictionary<string, object>();
        secretsDict[secName] = secretSubDict;

        if (secret.StringData is { Count: > 0 })
        {
            // For each secret key, store the default in .Values.secrets.<secName>.<key>
            // and reference a Helm placeholder in stringData.
            foreach (var (key, value) in secret.StringData)
            {
                var defaultValue = value ?? string.Empty;

                secretSubDict[key] = defaultValue;
                stringDataDict[key] = SecretValuePlaceholder(secName, key);
            }
        }

        return stringDataDict;
    }

    private static void AssignContainerToImages(Dictionary<string, object> valuesObj, V1Container container, string containerName)
    {
        if (!valuesObj.TryGetValue(KubernetesPublisherManifestKeys.Images, out var imagesObj))
        {
            imagesObj = new Dictionary<string, object>();
            valuesObj[KubernetesPublisherManifestKeys.Images] = imagesObj;
        }

        if (imagesObj is not Dictionary<string, object> imagesDict)
        {
            throw new DistributedApplicationException("Expected .Values.images to be a dictionary.");
        }

        var imageRepo = container.Image ?? throw new DistributedApplicationException("Container image is null, and cannot be published.");
        var imageTag = "latest";
        if (imageRepo.Contains(':'))
        {
            var idx = imageRepo.LastIndexOf(':');
            imageTag = imageRepo[(idx + 1)..];
            imageRepo = imageRepo[..idx];
        }
        
        // Add/overwrite an entry in .Values.images for this container
        imagesDict[containerName] = new Dictionary<string, object>
        {
            [KubernetesPublisherManifestKeys.Repository] = imageRepo,
            [KubernetesPublisherManifestKeys.Tag] = imageTag,
            [KubernetesPublisherManifestKeys.PullPolicy] =
                !string.IsNullOrEmpty(container.ImagePullPolicy)
                    ? container.ImagePullPolicy
                    : "IfNotPresent",
        };
    }
}

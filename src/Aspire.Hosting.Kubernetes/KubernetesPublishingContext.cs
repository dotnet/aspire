// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Kubernetes.Extensions;
using Aspire.Hosting.Kubernetes.Resources;
using Aspire.Hosting.Kubernetes.Yaml;
using Aspire.Hosting.Yaml;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Aspire.Hosting.Kubernetes;

internal sealed class KubernetesPublishingContext(
    DistributedApplicationExecutionContext executionContext,
    KubernetesPublisherOptions publisherOptions,
    ILogger logger,
    CancellationToken cancellationToken = default)
{
    private readonly Dictionary<string, Dictionary<string, object>> _helmValues = new()
    {
        [HelmExtensions.ParametersKey] = new Dictionary<string, object>(),
        [HelmExtensions.SecretsKey] = new Dictionary<string, object>(),
        [HelmExtensions.ConfigKey] = new Dictionary<string, object>(),
    };

    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithTypeConverter(new ByteArrayStringYamlConverter())
        .WithTypeConverter(new IntOrStringYamlConverter())
        .WithEventEmitter(nextEmitter => new ForceQuotedStringsEventEmitter(nextEmitter))
        .WithEventEmitter(e => new FloatEmitter(e))
        .WithEmissionPhaseObjectGraphVisitor(args => new YamlIEnumerableSkipEmptyObjectGraphVisitor(args.InnerVisitor))
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .WithNewLine("\n")
        .WithIndentedSequences()
        .Build();

    public ILogger Logger => logger;

    internal async Task WriteModelAsync(DistributedApplicationModel model)
    {
        if (!executionContext.IsPublishMode)
        {
            logger.NotInPublishingMode();
            return;
        }

        logger.StartGeneratingKubernetes();

        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(publisherOptions.OutputPath);

        if (model.Resources.Count == 0)
        {
            logger.EmptyModel();
            return;
        }

        await WriteKubernetesOutputAsync(model).ConfigureAwait(false);

        logger.FinishGeneratingKubernetes(publisherOptions.OutputPath);
    }

    private async Task WriteKubernetesOutputAsync(DistributedApplicationModel model)
    {
        var kubernetesEnvironments = model.Resources.OfType<KubernetesEnvironmentResource>().ToArray();

        if (kubernetesEnvironments.Length > 1)
        {
            throw new NotSupportedException("Multiple Kubernetes environments are not supported.");
        }

        var environment = kubernetesEnvironments.FirstOrDefault();

        if (environment == null)
        {
            // No Kubernetes environment found
            throw new InvalidOperationException($"No Kubernetes environment found. Ensure a Kubernetes environment is registered by calling {nameof(KubernetesEnvironmentExtensions.AddKubernetesEnvironment)}.");
        }

        foreach (var resource in model.Resources)
        {
            if (resource.GetDeploymentTargetAnnotation()?.DeploymentTarget is KubernetesResource serviceResource)
            {
                if (serviceResource.TargetResource.TryGetAnnotationsOfType<KubernetesServiceCustomizationAnnotation>(out var annotations))
                {
                    foreach (var a in annotations)
                    {
                        a.Configure(serviceResource);
                    }
                }

                await WriteKubernetesTemplatesForResource(resource, serviceResource.TemplatedResources).ConfigureAwait(false);
                AppendResourceContextToHelmValues(resource, serviceResource);
            }
        }

        await WriteKubernetesHelmChartAsync().ConfigureAwait(false);
        await WriteKubernetesHelmValuesAsync().ConfigureAwait(false);
    }

    private void AppendResourceContextToHelmValues(IResource resource, KubernetesResource resourceContext)
    {
        AddValuesToHelmSection(resource, resourceContext.Parameters, HelmExtensions.ParametersKey);
        AddValuesToHelmSection(resource, resourceContext.EnvironmentVariables, HelmExtensions.ConfigKey);
        AddValuesToHelmSection(resource, resourceContext.Secrets, HelmExtensions.SecretsKey);
    }

    private void AddValuesToHelmSection(
        IResource resource,
        Dictionary<string, KubernetesResource.HelmExpressionWithValue> contextItems,
        string helmKey)
    {
        if (contextItems.Count <= 0 || _helmValues[helmKey] is not Dictionary<string, object> helmSection)
        {
            return;
        }

        var paramValues = new Dictionary<string, string>();

        foreach (var (key, helmExpressionWithValue) in contextItems)
        {
            if (helmExpressionWithValue.ValueContainsHelmExpression)
            {
                continue;
            }

            paramValues[key] = helmExpressionWithValue.Value ?? string.Empty;
        }

        if (paramValues.Count > 0)
        {
            helmSection[resource.Name] = paramValues;
        }
    }

    private async Task WriteKubernetesTemplatesForResource(IResource resource, List<BaseKubernetesResource> templatedItems)
    {
        var templatesFolder = Path.Combine(publisherOptions.OutputPath!, "templates", resource.Name);
        Directory.CreateDirectory(templatesFolder);

        foreach (var templatedItem in templatedItems)
        {
            var fileName = $"{templatedItem.GetType().Name.ToLowerInvariant()}.yaml";
            var outputFile = Path.Combine(templatesFolder, fileName);
            var yaml = _serializer.Serialize(templatedItem);

            using var writer = new StreamWriter(outputFile);
            await writer.WriteLineAsync(HelmExtensions.TemplateFileSeparator).ConfigureAwait(false);
            await writer.WriteAsync(yaml).ConfigureAwait(false);
        }
    }

    private async Task WriteKubernetesHelmValuesAsync()
    {
        var valuesYaml = _serializer.Serialize(_helmValues);
        var outputFile = Path.Combine(publisherOptions.OutputPath!, "values.yaml");
        Directory.CreateDirectory(publisherOptions.OutputPath!);
        await File.WriteAllTextAsync(outputFile, valuesYaml, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteKubernetesHelmChartAsync()
    {
        var helmChart = new HelmChart
        {
            Name = publisherOptions.HelmChartName,
            Version = publisherOptions.HelmChartVersion,
            AppVersion = publisherOptions.HelmChartVersion,
            Description = publisherOptions.HelmChartDescription,
            Type = "application",
            ApiVersion = "v2",
            Keywords = ["aspire", "kubernetes"],
            KubeVersion = ">= 1.18.0-0",
        };

        var chartYaml = _serializer.Serialize(helmChart);
        var outputFile = Path.Combine(publisherOptions.OutputPath!, "Chart.yaml");
        Directory.CreateDirectory(publisherOptions.OutputPath!);
        await File.WriteAllTextAsync(outputFile, chartYaml, cancellationToken).ConfigureAwait(false);
    }
}

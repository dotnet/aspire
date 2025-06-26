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
    string outputPath,
    ILogger logger,
    CancellationToken cancellationToken = default)
{
    private string RootOutputPath { get; } = outputPath;

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

    private string GetOutputPath(KubernetesEnvironmentResource environment) =>
        Path.Combine(RootOutputPath, environment.Name);

    internal async Task WriteModelAsync(DistributedApplicationModel model, KubernetesEnvironmentResource environment)
    {
        if (!executionContext.IsPublishMode)
        {
            logger.NotInPublishingMode();
            return;
        }

        logger.StartGeneratingKubernetes();

        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(RootOutputPath);

        if (model.Resources.Count == 0)
        {
            logger.EmptyModel();
            return;
        }

        await WriteKubernetesOutputAsync(model, environment).ConfigureAwait(false);

        logger.FinishGeneratingKubernetes(RootOutputPath);
    }

    private async Task WriteKubernetesOutputAsync(DistributedApplicationModel model, KubernetesEnvironmentResource environment)
    {
        foreach (var resource in model.Resources)
        {
            if (resource.GetDeploymentTargetAnnotation(environment)?.DeploymentTarget is KubernetesResource serviceResource)
            {
                if (serviceResource.TargetResource.TryGetAnnotationsOfType<KubernetesServiceCustomizationAnnotation>(out var annotations))
                {
                    foreach (var a in annotations)
                    {
                        a.Configure(serviceResource);
                    }
                }

                await WriteKubernetesTemplatesForResource(environment, resource, serviceResource.GetTemplatedResources()).ConfigureAwait(false);
                AppendResourceContextToHelmValues(resource, serviceResource);
            }
        }

        await WriteKubernetesHelmChartAsync(environment).ConfigureAwait(false);
        await WriteKubernetesHelmValuesAsync(environment).ConfigureAwait(false);
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
            helmSection[resource.Name.ToHelmValuesSectionName()] = paramValues;
        }
    }

    private async Task WriteKubernetesTemplatesForResource(KubernetesEnvironmentResource environment, IResource resource, IEnumerable<BaseKubernetesResource> templatedItems)
    {
        var templatesFolder = Path.Combine(GetOutputPath(environment), "templates", resource.Name);
        Directory.CreateDirectory(templatesFolder);

        foreach (var templatedItem in templatedItems)
        {
            var fileName = GetFilename(resource.Name, templatedItem);
            var outputFile = Path.Combine(templatesFolder, fileName);
            var yaml = _serializer.Serialize(templatedItem);

            using var writer = new StreamWriter(outputFile);
            await writer.WriteLineAsync(HelmExtensions.TemplateFileSeparator).ConfigureAwait(false);
            await writer.WriteAsync(yaml).ConfigureAwait(false);
        }
    }

    private static string GetFilename(string baseName, BaseKubernetesResource templatedItem)
    {
        if (string.IsNullOrWhiteSpace(templatedItem.Metadata.Name))
        {
            return $"{templatedItem.GetType().Name.ToLowerInvariant()}.yaml";
        }

        var resourceName = templatedItem.Metadata.Name;
        if (resourceName.StartsWith($"{baseName.ToLowerInvariant()}-"))
        {
            resourceName = resourceName.Substring(baseName.Length + 1); // +1 for the hyphen
        }

        return $"{resourceName}.yaml";
    }

    private async Task WriteKubernetesHelmValuesAsync(KubernetesEnvironmentResource environment)
    {
        var valuesYaml = _serializer.Serialize(_helmValues);
        var outputPath = GetOutputPath(environment);
        var outputFile = Path.Combine(outputPath, "values.yaml");
        Directory.CreateDirectory(outputPath);
        await File.WriteAllTextAsync(outputFile, valuesYaml, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteKubernetesHelmChartAsync(KubernetesEnvironmentResource environment)
    {
        var helmChart = new HelmChart
        {
            Name = environment.HelmChartName,
            Version = environment.HelmChartVersion,
            AppVersion = environment.HelmChartVersion,
            Description = environment.HelmChartDescription,
            Type = "application",
            ApiVersion = "v2",
            Keywords = ["aspire", "kubernetes"],
            KubeVersion = ">= 1.18.0-0",
        };

        var chartYaml = _serializer.Serialize(helmChart);
        var outputPath = GetOutputPath(environment);
        var outputFile = Path.Combine(outputPath, "Chart.yaml");
        Directory.CreateDirectory(outputPath);
        await File.WriteAllTextAsync(outputFile, chartYaml, cancellationToken).ConfigureAwait(false);
    }
}

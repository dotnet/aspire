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
    private readonly Dictionary<IResource, KubernetesResourceContext> _kubernetesComponents = [];

    private readonly Dictionary<string, object> _helmValues = new()
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
        foreach (var resource in model.Resources)
        {
            if (resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var lastAnnotation) &&
                lastAnnotation == ManifestPublishingCallbackAnnotation.Ignore)
            {
                continue;
            }

            if (!resource.IsContainer() && resource is not ProjectResource)
            {
                continue;
            }

            var kubernetesComponentContext = await ProcessResourceAsync(resource).ConfigureAwait(false);
            kubernetesComponentContext.BuildKubernetesResources();

            await WriteKubernetesTemplatesForResource(resource, kubernetesComponentContext.TemplatedResources).ConfigureAwait(false);
            AppendResourceContextToHelmValues(resource, kubernetesComponentContext);
        }

        await WriteKubernetesHelmChartAsync().ConfigureAwait(false);
        await WriteKubernetesHelmValuesAsync().ConfigureAwait(false);
    }

    private void AppendResourceContextToHelmValues(IResource resource, KubernetesResourceContext resourceContext)
    {
        AddValuesToHelmSection(resource, resourceContext.Parameters, HelmExtensions.ParametersKey);
        AddValuesToHelmSection(resource, resourceContext.EnvironmentVariables, HelmExtensions.ConfigKey);
        AddValuesToHelmSection(resource, resourceContext.Secrets, HelmExtensions.SecretsKey);
    }

    private void AddValuesToHelmSection(
        IResource resource,
        Dictionary<string, KubernetesResourceContext.HelmExpressionWithValue> contextItems,
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

    internal async Task<KubernetesResourceContext> ProcessResourceAsync(IResource resource)
    {
        if (!_kubernetesComponents.TryGetValue(resource, out var context))
        {
            _kubernetesComponents[resource] = context = new(resource, this, publisherOptions);
            await context.ProcessResourceAsync(executionContext, cancellationToken).ConfigureAwait(false);
        }

        return context;
    }
}

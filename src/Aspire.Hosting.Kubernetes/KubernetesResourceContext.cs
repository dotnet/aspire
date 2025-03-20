// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Kubernetes.Extensions;
using Aspire.Hosting.Kubernetes.Resources;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Kubernetes;

internal sealed class KubernetesResourceContext(
    IResource resource,
    KubernetesPublishingContext kubernetesPublishingContext,
    KubernetesPublisherOptions publisherOptions)
{
    internal record struct EndpointMapping(string Scheme, string Host, int InternalPort, int ExposedPort, string Name);
    internal record struct HelmExpressionValue(string Expression, string? Value);
    public readonly Dictionary<string, EndpointMapping> EndpointMappings = [];
    public readonly Dictionary<string, HelmExpressionValue> EnvironmentVariables = [];
    public readonly Dictionary<string, HelmExpressionValue> Secrets = [];
    public readonly Dictionary<string, HelmExpressionValue> Parameters = [];
    public Dictionary<string, string> Labels = [];
    public List<BaseKubernetesResource> TemplatedResources { get; } = [];
    internal List<string> Commands { get; } = [];
    internal List<VolumeMountV1> Volumes { get; } = [];
    internal IResource Resource => resource;
    internal ILogger Logger => kubernetesPublishingContext.Logger;
    internal KubernetesPublisherOptions PublisherOptions => publisherOptions;

    public void BuildKubernetesResources()
    {
        SetLabels();
        CreateApplication();
        AddIfExists(resource.ToConfigMap(this));
        AddIfExists(resource.ToSecret(this));
        AddIfExists(resource.ToService(this));
    }

    private void SetLabels()
    {
        Labels = new()
        {
            ["app"] = "aspire",
            ["component"] = resource.Name,
        };
    }

    private void CreateApplication()
    {
        if (resource is IResourceWithConnectionString)
        {
            var statefulSet = resource.ToStatefulSet(this);
            TemplatedResources.Add(statefulSet);
            return;
        }

        var deployment = resource.ToDeployment(this);
        TemplatedResources.Add(deployment);
    }

    private void AddIfExists(BaseKubernetesResource? instance)
    {
        if (instance is not null)
        {
            TemplatedResources.Add(instance);
        }
    }

    internal bool TryGetContainerImageName(IResource resourceInstance, out string? containerImageName)
    {
        if (!resourceInstance.TryGetLastAnnotation<DockerfileBuildAnnotation>(out _) && resourceInstance is not ProjectResource)
        {
            return resourceInstance.TryGetContainerImageName(out containerImageName);
        }

        var imageEnvName = $"{resourceInstance.Name.ToManifestFriendlyResourceName()}_image";
        var value = $"{resourceInstance.Name}:latest";
        var expression = imageEnvName.ToHelmParameterExpression(resource.Name);

        Parameters[imageEnvName] = new HelmExpressionValue(expression, value);
        containerImageName = expression;
        return false;

    }

    public async Task ProcessResourceAsync(DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        ProcessEndpoints();
        ProcessVolumes();

        await ProcessEnvironmentAsync(executionContext, cancellationToken).ConfigureAwait(false);
        await ProcessArgumentsAsync(cancellationToken).ConfigureAwait(false);
    }

    private void ProcessEndpoints()
    {
        if (!resource.TryGetEndpoints(out var endpoints))
        {
            return;
        }

        foreach (var endpoint in endpoints)
        {
            var port = endpoint.TargetPort ?? 80;

            EndpointMappings[endpoint.Name] = new(endpoint.UriScheme, resource.Name, port, port, endpoint.Name);
        }
    }

    private void ProcessVolumes()
    {
        if (!resource.TryGetContainerMounts(out var mounts))
        {
            return;
        }

        foreach (var volume in mounts)
        {
            if (volume.Source is null || volume.Target is null)
            {
                throw new InvalidOperationException("Volume source and target must be set");
            }

            if (volume.Type == ContainerMountType.BindMount)
            {
                throw new InvalidOperationException("Bind mounts are not supported by the Kubernetes publisher");
            }

            var newVolume = new VolumeMountV1
            {
                Name = volume.Source,
                ReadOnly = volume.IsReadOnly,
                MountPath = volume.Target,
            };

            Volumes.Add(newVolume);
        }
    }

    private async Task ProcessArgumentsAsync(CancellationToken cancellationToken)
    {
        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var commandLineArgsCallbackAnnotations))
        {
            var context = new CommandLineArgsCallbackContext([], cancellationToken: cancellationToken);

            foreach (var c in commandLineArgsCallbackAnnotations)
            {
                await c.Callback(context).ConfigureAwait(false);
            }

            foreach (var arg in context.Args)
            {
                var value = await ProcessValueAsync(arg).ConfigureAwait(false);

                if (value is not string str)
                {
                    throw new NotSupportedException("Command line args must be strings");
                }

                Commands.Add(new(str));
            }
        }
    }

    private async Task ProcessEnvironmentAsync(DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        if (resource.TryGetAnnotationsOfType<EnvironmentCallbackAnnotation>(out var environmentCallbacks))
        {
            var context = new EnvironmentCallbackContext(executionContext, cancellationToken: cancellationToken);

            foreach (var c in environmentCallbacks)
            {
                await c.Callback(context).ConfigureAwait(false);
            }

            var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in context.EnvironmentVariables)
            {
                var value = await ProcessValueAsync(kv.Value).ConfigureAwait(false);
                var key = kv.Key.ToManifestFriendlyResourceName();
                var stringValue = value.ToString() ?? string.Empty;

                if (processedKeys.Contains(key))
                {
                    continue;
                }

                // Move connection strings to secrets
                if (key.IsConnectionString())
                {
                    var expression = key.ToHelmSecretExpression(resource.Name);
                    Secrets[key] = new HelmExpressionValue(expression, stringValue);
                    continue;
                }

                if (stringValue.IsHelmSecretExpression())
                {
                    // If the value references secrets, it belongs in secrets
                    var expression = key.ToHelmSecretExpression(resource.Name);
                    Secrets[key] = new HelmExpressionValue(expression, stringValue);
                    continue;
                }

                // All other values go to environment variables
                var configExpression = key.ToHelmConfigExpression(resource.Name);
                EnvironmentVariables[key] = new HelmExpressionValue(configExpression, stringValue);
                processedKeys.Add(key);
            }
        }
    }

    private static string GetEndpointValue(EndpointMapping mapping, EndpointProperty property)
    {
        var (scheme, host, internalPort, exposedPort, _) = mapping;

        return property switch
        {
            EndpointProperty.Url => GetHostValue($"{scheme}://", suffix: $":{internalPort}"),
            EndpointProperty.Host or EndpointProperty.IPV4Host => GetHostValue(),
            EndpointProperty.Port => internalPort.ToString(CultureInfo.InvariantCulture),
            EndpointProperty.HostAndPort => GetHostValue(suffix: $":{internalPort}"),
            EndpointProperty.TargetPort => $"{exposedPort}",
            EndpointProperty.Scheme => scheme,
            _ => throw new NotSupportedException(),
        };

        string GetHostValue(string? prefix = null, string? suffix = null)
        {
            return $"{prefix}{host}{suffix}";
        }
    }

    private async Task<object> ProcessValueAsync(object value)
    {
        while (true)
        {
            if (value is string s)
            {
                return s;
            }

            if (value is EndpointReference ep)
            {
                var context = ep.Resource == resource
                    ? this
                    : await kubernetesPublishingContext.ProcessResourceAsync(ep.Resource)
                        .ConfigureAwait(false);

                var mapping = context.EndpointMappings[ep.EndpointName];

                var url = GetEndpointValue(mapping, EndpointProperty.Url);

                return url;
            }

            if (value is ParameterResource param)
            {
                return AllocateParameter(param);
            }

            if (value is ConnectionStringReference cs)
            {
                value = cs.Resource.ConnectionStringExpression;
                continue;
            }

            if (value is IResourceWithConnectionString csrs)
            {
                value = csrs.ConnectionStringExpression;
                continue;
            }

            if (value is EndpointReferenceExpression epExpr)
            {
                var context = epExpr.Endpoint.Resource == resource
                    ? this
                    : await kubernetesPublishingContext.ProcessResourceAsync(epExpr.Endpoint.Resource).ConfigureAwait(false);

                var mapping = context.EndpointMappings[epExpr.Endpoint.EndpointName];

                var val = GetEndpointValue(mapping, epExpr.Property);

                return val;
            }

            if (value is ReferenceExpression expr)
            {
                if (expr is {Format: "{0}", ValueProviders.Count: 1})
                {
                    return (await ProcessValueAsync(expr.ValueProviders[0]).ConfigureAwait(false)).ToString() ?? string.Empty;
                }

                var args = new object[expr.ValueProviders.Count];
                var index = 0;

                foreach (var vp in expr.ValueProviders)
                {
                    var val = await ProcessValueAsync(vp).ConfigureAwait(false);
                    args[index++] = val ?? throw new InvalidOperationException("Value is null");
                }

                return string.Format(CultureInfo.InvariantCulture, expr.Format, args);
            }

            // If we don't know how to process the value, we just return it as an external reference
            if (value is IManifestExpressionProvider r)
            {
                kubernetesPublishingContext.Logger.NotSupportedResourceWarning(nameof(value), r.GetType().Name);

                return ResolveUnknownValue(r);
            }

            throw new NotSupportedException($"Unsupported value type: {value.GetType().Name}");
        }
    }

    private string AllocateParameter(ParameterResource parameter)
    {
        var formattedName = parameter.Name.ToManifestFriendlyResourceName();
        var value = parameter.Default is null ? null : parameter.Value;

        if (parameter.Secret)
        {
            var expression = formattedName.ToHelmSecretExpression(resource.Name);
            Secrets[formattedName] = new HelmExpressionValue(expression, value);
            return expression;
        }

        // For non-secret parameters, store in config map
        var configExpression = formattedName.ToHelmConfigExpression(resource.Name);
        var configValue = parameter.Default is null ? null : parameter.Value;
        EnvironmentVariables[formattedName] = new HelmExpressionValue(configExpression, configValue);
        return configValue ?? configExpression;
    }

    private string ResolveUnknownValue(IManifestExpressionProvider parameter)
    {
        var formattedName = parameter.ValueExpression.Replace("{", "")
            .Replace("}", "")
            .Replace(".", "_")
            .ToManifestFriendlyResourceName();

        var value = parameter.ValueExpression;

        // If the value contains Helm expressions
        if (value.IsHelmExpression())
        {
            // Store in secrets if it references secrets, otherwise in config
            if (value.IsHelmSecretExpression())
            {
                var expression = formattedName.ToHelmSecretExpression(resource.Name);
                Secrets[formattedName] = new HelmExpressionValue(expression, value);
                return expression;
            }

            var configExpression = formattedName.ToHelmConfigExpression(resource.Name);
            EnvironmentVariables[formattedName] = new HelmExpressionValue(configExpression, value);
            return configExpression;
        }

        // For values without Helm expressions
        var targetExpression = formattedName.ToHelmConfigExpression(resource.Name);
        EnvironmentVariables[formattedName] = new HelmExpressionValue(targetExpression, value);
        return targetExpression;
    }
}

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
    internal record EndpointMapping(string Scheme, string Host, string Port, string Name, string? HelmExpression = null);
    public readonly Dictionary<string, EndpointMapping> EndpointMappings = [];
    public readonly Dictionary<string, HelmExpressionWithValue> EnvironmentVariables = [];
    public readonly Dictionary<string, HelmExpressionWithValue> Secrets = [];
    public readonly Dictionary<string, HelmExpressionWithValue> Parameters = [];
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

        Parameters[imageEnvName] = new(expression, value);
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
            if (resource is ProjectResource && endpoint.TargetPort is null)
            {
                GenerateDefaultProjectEndpointMapping(endpoint);
                continue;
            }

            var port = endpoint.TargetPort ?? throw new InvalidOperationException($"Unable to resolve port {endpoint.TargetPort} for endpoint {endpoint.Name} on resource {resource.Name}");
            var portValue = port.ToString(CultureInfo.InvariantCulture);
            EndpointMappings[endpoint.Name] = new(endpoint.UriScheme, resource.Name, portValue, endpoint.Name);
        }
    }

    private void GenerateDefaultProjectEndpointMapping(EndpointAnnotation endpoint)
    {
        const string defaultPort = "8080";

        var paramName = $"port_{endpoint.Name}".ToManifestFriendlyResourceName();

        var helmExpression = paramName.ToHelmParameterExpression(resource.Name);
        Parameters[paramName] = new(helmExpression, defaultPort);

        var aspNetCoreUrlsExpression = "ASPNETCORE_URLS".ToHelmConfigExpression(resource.Name);
        EnvironmentVariables["ASPNETCORE_URLS"] = new(aspNetCoreUrlsExpression, $"http://+:${defaultPort}");

        EndpointMappings[endpoint.Name] = new(endpoint.UriScheme, resource.Name, helmExpression, endpoint.Name, helmExpression);
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
            var context = new EnvironmentCallbackContext(executionContext, resource, cancellationToken: cancellationToken);

            foreach (var c in environmentCallbacks)
            {
                await c.Callback(context).ConfigureAwait(false);
            }

            foreach (var environmentVariable in context.EnvironmentVariables)
            {
                var key = environmentVariable.Key.ToManifestFriendlyResourceName();
                var value = await ProcessValueAsync(environmentVariable.Value).ConfigureAwait(false);

                switch (value)
                {
                    case HelmExpressionWithValue helmExpression:
                        ProcessEnvironmentHelmExpression(helmExpression, key);
                        continue;
                    case string stringValue:
                        ProcessEnvironmentStringValue(stringValue, key, resource.Name);
                        continue;
                    default:
                        ProcessEnvironmentDefaultValue(value, key, resource.Name);
                        break;
                }
            }
        }
    }

    private void ProcessEnvironmentHelmExpression(HelmExpressionWithValue helmExpression, string key)
    {
        switch (helmExpression)
        {
            case { IsHelmSecretExpression: true, ValueContainsSecretExpression: false }:
                Secrets[key] = helmExpression;
                return;
            case { IsHelmSecretExpression: false, ValueContainsSecretExpression: false }:
                EnvironmentVariables[key] = helmExpression;
                break;
        }
    }

    private void ProcessEnvironmentStringValue(string stringValue, string key, string resourceName)
    {
        if (stringValue.ContainsHelmSecretExpression())
        {
            var secretExpression = stringValue.ToHelmSecretExpression(resourceName);
            Secrets[key] = new(secretExpression, stringValue);
            return;
        }

        var configExpression = key.ToHelmConfigExpression(resourceName);
        EnvironmentVariables[key] = new(configExpression, stringValue);
    }

    private void ProcessEnvironmentDefaultValue(object value, string key, string resourceName)
    {
        var configExpression = key.ToHelmConfigExpression(resourceName);
        EnvironmentVariables[key] = new(configExpression, value.ToString() ?? string.Empty);
    }

    private static string GetEndpointValue(EndpointMapping mapping, EndpointProperty property)
    {
        var (scheme, host, port, _, _) = mapping;

        return property switch
        {
            EndpointProperty.Url => GetHostValue($"{scheme}://", suffix: $":{port}"),
            EndpointProperty.Host or EndpointProperty.IPV4Host => GetHostValue(),
            EndpointProperty.Port => port,
            EndpointProperty.HostAndPort => GetHostValue(suffix: $":{port}"),
            EndpointProperty.TargetPort => port,
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

    private HelmExpressionWithValue AllocateParameter(ParameterResource parameter)
    {
        var formattedName = parameter.Name.ToManifestFriendlyResourceName();

        var expression = parameter.Secret ?
            formattedName.ToHelmSecretExpression(resource.Name) :
            formattedName.ToHelmConfigExpression(resource.Name);

        var value = parameter.Default is null ? null : parameter.Value;
        return new(expression, value);
    }

    private HelmExpressionWithValue ResolveUnknownValue(IManifestExpressionProvider parameter)
    {
        var formattedName = parameter.ValueExpression.Replace("{", "")
            .Replace("}", "")
            .Replace(".", "_")
            .ToManifestFriendlyResourceName();

        var helmExpression = parameter.ValueExpression.ContainsHelmSecretExpression() ?
            formattedName.ToHelmSecretExpression(resource.Name) :
            formattedName.ToHelmConfigExpression(resource.Name);

        return new(helmExpression, parameter.ValueExpression);
    }

    internal class HelmExpressionWithValue(string helmExpression, string? value)
    {
        public string HelmExpression { get; } = helmExpression;
        public string? Value { get; } = value;
        public bool IsHelmSecretExpression => HelmExpression.ContainsHelmSecretExpression();
        public bool ValueContainsSecretExpression => Value?.ContainsHelmSecretExpression() ?? false;
        public bool ValueContainsHelmExpression => Value?.ContainsHelmExpression() ?? false;
        public override string ToString() => Value ?? HelmExpression;
    }
}

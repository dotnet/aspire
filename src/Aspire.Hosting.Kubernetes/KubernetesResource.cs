// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Kubernetes.Extensions;
using Aspire.Hosting.Kubernetes.Resources;

namespace Aspire.Hosting.Kubernetes;

/// <summary>
/// Represents a compute resource for Kubernetes.
/// </summary>
public class KubernetesResource(string name, IResource resource, KubernetesEnvironmentResource kubernetesEnvironmentResource) : Resource(name), IResourceWithParent<KubernetesEnvironmentResource>
{
    /// <inheritdoc/>
    public KubernetesEnvironmentResource Parent => kubernetesEnvironmentResource;

    internal record EndpointMapping(string Scheme, string Host, string Port, string Name, string? HelmExpression = null);
    internal Dictionary<string, EndpointMapping> EndpointMappings { get; } = [];
    internal Dictionary<string, object> RawEnvironmentVariables { get; } = [];
    internal List<object> RawArguments { get; } = [];
    internal Dictionary<string, HelmExpressionWithValue> EnvironmentVariables { get; } = [];
    internal Dictionary<string, HelmExpressionWithValue> Secrets { get; } = [];
    internal Dictionary<string, HelmExpressionWithValue> Parameters { get; } = [];
    internal Dictionary<string, string> Labels { get; private set; } = [];
    internal List<string> Commands { get; } = [];
    internal List<VolumeMountV1> Volumes { get; } = [];
    internal List<PersistentVolume> PersistentVolumes { get; } = [];
    internal List<PersistentVolumeClaim> PersistentVolumeClaims { get; } = [];

    /// <summary>
    /// </summary>
    public Workload? Workload { get; set; }

    /// <summary>
    /// Gets or sets the Kubernetes ConfigMap associated with this resource.
    /// </summary>
    public ConfigMap? ConfigMap { get; set; }

    /// <summary>
    /// Gets or sets the Kubernetes Secret associated with this resource.
    /// </summary>
    public Secret? Secret { get; set; }

    /// <summary>
    /// Gets or sets the Kubernetes Service associated with this resource.
    /// </summary>
    public Service? Service { get; set; }

    /// <summary>
    /// Additional resources that are part of this Kubernetes service.
    /// </summary>
    public List<BaseKubernetesResource> AdditionalResources { get; } = [];

    /// <summary>
    /// Gets the resource that is the target of this Kubernetes service.
    /// </summary>
    internal IResource TargetResource => resource;

    internal IEnumerable<BaseKubernetesResource> GetTemplatedResources()
    {
        if (Workload is not null)
        {
            yield return Workload;
        }

        if (ConfigMap is not null)
        {
            yield return ConfigMap;
        }
        if (Secret is not null)
        {
            yield return Secret;
        }
        if (Service is not null)
        {
            yield return Service;
        }

        foreach (var volume in PersistentVolumes)
        {
            yield return volume;
        }

        foreach (var volumeClaim in PersistentVolumeClaims)
        {
            yield return volumeClaim;
        }

        foreach (var resource in AdditionalResources)
        {
            yield return resource;
        }
    }

    private void BuildKubernetesResources(KubernetesEnvironmentContext context, DistributedApplicationExecutionContext executionContext)
    {
        // Process deferred environment variables
        foreach (var environmentVariable in RawEnvironmentVariables)
        {
            var key = environmentVariable.Key.ToHelmValuesSectionName();
            var value = this.ProcessValue(context, executionContext, environmentVariable.Value);

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

        // Process deferred arguments
        foreach (var arg in RawArguments)
        {
            var value = this.ProcessValue(context, executionContext, arg);

            if (value is not string str)
            {
                throw new NotSupportedException("Command line args must be strings");
            }

            Commands.Add(str);
        }

        SetLabels();
        CreateApplication();
        ConfigMap = resource.ToConfigMap(this);
        Secret = resource.ToSecret(this);
        Service = resource.ToService(this);
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
            Workload = resource.ToStatefulSet(this);
            return;
        }

        Workload = resource.ToDeployment(this);
    }

    internal string GetContainerImageName(IResource resourceInstance)
    {
        if (!resourceInstance.TryGetLastAnnotation<DockerfileBuildAnnotation>(out _) && resourceInstance is not ProjectResource)
        {
            if (resourceInstance.TryGetContainerImageName(out var containerImageName))
            {
                return containerImageName;
            }
        }

        var imageEnvName = $"{resourceInstance.Name.ToHelmValuesSectionName()}_image";
        var value = $"{resourceInstance.Name}:latest";
        var expression = imageEnvName.ToHelmParameterExpression(resource.Name);

        Parameters[imageEnvName] = new(expression, value);
        return expression;
    }

    internal async Task ProcessResourceAsync(KubernetesEnvironmentContext context, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        ProcessEndpoints();
        ProcessVolumes();
        await ProcessEnvironmentAsync(executionContext, cancellationToken).ConfigureAwait(false);
        await ProcessArgumentsAsync(executionContext, cancellationToken).ConfigureAwait(false);
        
        BuildKubernetesResources(context, executionContext);
    }

    private void ProcessEndpoints()
    {
        if (!resource.TryGetEndpoints(out var endpoints))
        {
            return;
        }

        foreach (var endpoint in endpoints)
        {
            if (endpoint.TargetPort is null)
            {
                const string defaultPort = "8080";

                var paramName = $"port_{endpoint.Name}".ToHelmValuesSectionName();

                var helmExpression = paramName.ToHelmParameterExpression(resource.Name);
                Parameters[paramName] = new(helmExpression, defaultPort);

                EndpointMappings[endpoint.Name] = new(endpoint.UriScheme, resource.Name, helmExpression, endpoint.Name, helmExpression);
                continue;
            }

            var port = endpoint.TargetPort ?? throw new InvalidOperationException($"Unable to resolve port {endpoint.TargetPort} for endpoint {endpoint.Name} on resource {resource.Name}");
            var portValue = port.ToString(CultureInfo.InvariantCulture);
            EndpointMappings[endpoint.Name] = new(endpoint.UriScheme, resource.Name, portValue, endpoint.Name);
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

    private async Task ProcessArgumentsAsync(DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var commandLineArgsCallbackAnnotations))
        {
            var context = new CommandLineArgsCallbackContext(RawArguments, cancellationToken: cancellationToken)
            {
                ExecutionContext = executionContext
            };

            foreach (var c in commandLineArgsCallbackAnnotations)
            {
                await c.Callback(context).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessEnvironmentAsync(DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        if (resource.TryGetAnnotationsOfType<EnvironmentCallbackAnnotation>(out var environmentCallbacks))
        {
            var context = new EnvironmentCallbackContext(executionContext, resource, RawEnvironmentVariables, cancellationToken: cancellationToken);

            foreach (var c in environmentCallbacks)
            {
                await c.Callback(context).ConfigureAwait(false);
            }
        }
    }

    internal object ProcessValue(KubernetesEnvironmentContext context, DistributedApplicationExecutionContext executionContext, object value)
    {
        while (true)
        {
            if (value is string s)
            {
                return s;
            }

            if (value is EndpointReference ep)
            {
                var referencedResource = ep.Resource == resource
                    ? this
                    : context.CreateKubernetesResourceAsync(ep.Resource, executionContext, default).GetAwaiter().GetResult();

                var mapping = referencedResource.EndpointMappings[ep.EndpointName];

                var url = GetEndpointValue(mapping, EndpointProperty.Url);

                return url;
            }

            if (value is ParameterResource param)
            {
                return AllocateParameter(param, resource);
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
                var referencedResource = epExpr.Endpoint.Resource == resource
                    ? this
                    : context.CreateKubernetesResourceAsync(epExpr.Endpoint.Resource, executionContext, default).GetAwaiter().GetResult();

                var mapping = referencedResource.EndpointMappings[epExpr.Endpoint.EndpointName];

                var val = GetEndpointValue(mapping, epExpr.Property);

                return val;
            }

            if (value is ReferenceExpression expr)
            {
                if (expr is { Format: "{0}", ValueProviders.Count: 1 })
                {
                    return this.ProcessValue(context, executionContext, expr.ValueProviders[0]).ToString() ?? string.Empty;
                }

                var args = new object[expr.ValueProviders.Count];
                var index = 0;

                foreach (var vp in expr.ValueProviders)
                {
                    var val = this.ProcessValue(context, executionContext, vp);
                    args[index++] = val ?? throw new InvalidOperationException("Value is null");
                }

                return string.Format(CultureInfo.InvariantCulture, expr.Format, args);
            }

            // If we don't know how to process the value, we just return it as an external reference
            if (value is IManifestExpressionProvider r)
            {
                return ResolveUnknownValue(r, resource);
            }

            throw new NotSupportedException($"Unsupported value type: {value.GetType().Name}");
        }
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

    private static HelmExpressionWithValue AllocateParameter(ParameterResource parameter, IResource resource)
    {
        var formattedName = parameter.Name.ToHelmValuesSectionName();

        var expression = parameter.Secret ?
            formattedName.ToHelmSecretExpression(resource.Name) :
            formattedName.ToHelmConfigExpression(resource.Name);

        var value = parameter.Default is null || parameter.Secret ? null : parameter.Value;
        return new(expression, value);
    }

    private static HelmExpressionWithValue ResolveUnknownValue(IManifestExpressionProvider parameter, IResource resource)
    {
        var formattedName = parameter.ValueExpression.Replace("{", "")
            .Replace("}", "")
            .Replace(".", "_")
            .ToHelmValuesSectionName();

        var helmExpression = parameter.ValueExpression.ContainsHelmSecretExpression() ?
            formattedName.ToHelmSecretExpression(resource.Name) :
            formattedName.ToHelmConfigExpression(resource.Name);

        return new(helmExpression, parameter.ValueExpression);
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

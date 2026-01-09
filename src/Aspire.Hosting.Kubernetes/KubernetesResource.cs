// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Kubernetes.Extensions;
using Aspire.Hosting.Kubernetes.Resources;

namespace Aspire.Hosting.Kubernetes;

/// <summary>
/// Represents a compute resource for Kubernetes.
/// </summary>
public partial class KubernetesResource(string name, IResource resource, KubernetesEnvironmentResource kubernetesEnvironmentResource) : Resource(name), IResourceWithParent<KubernetesEnvironmentResource>
{
    /// <inheritdoc/>
    public KubernetesEnvironmentResource Parent => kubernetesEnvironmentResource;

    internal record EndpointMapping(string Scheme, string Protocol, string Host, HelmValue Port, string Name, string? HelmExpression = null);
    internal Dictionary<string, EndpointMapping> EndpointMappings { get; } = [];
    internal Dictionary<string, HelmValue> EnvironmentVariables { get; } = [];
    internal Dictionary<string, HelmValue> Secrets { get; } = [];
    internal Dictionary<string, HelmValue> Parameters { get; } = [];
    internal Dictionary<string, string> Labels { get; private set; } = [];
    internal List<string> Commands { get; } = [];
    internal List<VolumeMountV1> Volumes { get; } = [];
    internal List<PersistentVolume> PersistentVolumes { get; } = [];
    internal List<PersistentVolumeClaim> PersistentVolumeClaims { get; } = [];
#pragma warning disable ASPIREPROBES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    internal List<(ProbeType Type, ProbeV1 Probe)> Probes { get; } = [];
#pragma warning restore ASPIREPROBES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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
            foreach (var label in Labels)
            {
                resource.Metadata.Labels.TryAdd(label.Key, label.Value);
            }

            yield return resource;
        }
    }

    private void BuildKubernetesResources()
    {
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
            ["app.kubernetes.io/name"] = Parent.HelmChartName,
            ["app.kubernetes.io/component"] = resource.Name,
            ["app.kubernetes.io/instance"] = ".Release.Name".ToHelmExpression(),
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
        var expression = new HelmValue(imageEnvName.ToHelmParameterExpression(resource.Name), value);

        Parameters[imageEnvName] = expression;
        return expression.ToScalar();
    }

    internal async Task ProcessResourceAsync(KubernetesEnvironmentContext context, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        ProcessEndpoints();
        ProcessVolumes();
        ProcessProbes();

        await ProcessEnvironmentAsync(context, executionContext, cancellationToken).ConfigureAwait(false);
        await ProcessArgumentsAsync(context, executionContext, cancellationToken).ConfigureAwait(false);

        BuildKubernetesResources();
    }

    private void ProcessEndpoints()
    {
        var resolvedEndpoints = resource.ResolveEndpoints(Parent.PortAllocator);

        foreach (var resolved in resolvedEndpoints)
        {
            var endpoint = resolved.Endpoint;

            if (resolved.TargetPort.Value is null)
            {
                // Default endpoint for ProjectResource - deployment tool assigns port
                GenerateDefaultProjectEndpointMapping(endpoint);
                continue;
            }

            var portValue = resolved.TargetPort.Value.Value.ToString(CultureInfo.InvariantCulture);
            EndpointMappings[endpoint.Name] = new(endpoint.UriScheme, GetKubernetesProtocolName(endpoint.Protocol), resource.Name.ToServiceName(), HelmValue.Literal(portValue), endpoint.Name);
        }
    }

    private void GenerateDefaultProjectEndpointMapping(EndpointAnnotation endpoint)
    {
        const int defaultPort = 8080;

        // Create a Helm parameter for the container port
        var paramName = $"port_{endpoint.Name}".ToHelmValuesSectionName();
        var helmValue = new HelmValue(
            paramName.ToHelmParameterExpression(resource.Name),
            defaultPort
        );
        Parameters[paramName] = helmValue;
        EndpointMappings[endpoint.Name] = new(endpoint.UriScheme, GetKubernetesProtocolName(endpoint.Protocol), resource.Name.ToServiceName(), helmValue, endpoint.Name);
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

#pragma warning disable ASPIREPROBES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private void ProcessProbes()
    {
        if (!resource.TryGetAnnotationsOfType<ProbeAnnotation>(out var probeAnnotations))
        {
            return;
        }

        foreach (var probeAnnotation in probeAnnotations)
        {
            ProbeV1? probe = null;
            if (probeAnnotation is EndpointProbeAnnotation endpointProbeAnnotation
                && EndpointMappings.TryGetValue(endpointProbeAnnotation.EndpointReference.EndpointName, out var endpointMapping))
            {
                probe = new ProbeV1()
                {
                    HttpGet = new()
                    {
                        Path = endpointProbeAnnotation.Path,
                        Port = GetEndpointValue(endpointMapping, EndpointProperty.TargetPort),
                        Scheme = endpointMapping.Scheme,
                    },
                };
            }

            if (probe is not null)
            {
                probe.InitialDelaySeconds = probeAnnotation.InitialDelaySeconds;
                probe.PeriodSeconds = probeAnnotation.PeriodSeconds;
                probe.TimeoutSeconds = probeAnnotation.TimeoutSeconds;
                probe.FailureThreshold = probeAnnotation.FailureThreshold;
                probe.SuccessThreshold = probeAnnotation.SuccessThreshold;

                Probes.Add((probeAnnotation.Type, probe));
            }
        }
    }
#pragma warning restore ASPIREPROBES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    private async Task ProcessArgumentsAsync(KubernetesEnvironmentContext environmentContext, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var commandLineArgsCallbackAnnotations))
        {
            var context = new CommandLineArgsCallbackContext([], resource, cancellationToken: cancellationToken);

            foreach (var c in commandLineArgsCallbackAnnotations)
            {
                await c.Callback(context).ConfigureAwait(false);
            }

            foreach (var arg in context.Args)
            {
                var value = await ProcessValueAsync(environmentContext, executionContext, arg).ConfigureAwait(false);

                if (value is not string str)
                {
                    throw new NotSupportedException("Command line args must be strings");
                }

                Commands.Add(new(str));
            }
        }
    }

    private async Task ProcessEnvironmentAsync(KubernetesEnvironmentContext environmentContext, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
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
                var key = environmentVariable.Key;
                var value = await ProcessValueAsync(environmentContext, executionContext, environmentVariable.Value).ConfigureAwait(false);

                switch (value)
                {
                    case HelmValue helmExpression:
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

    private void ProcessEnvironmentHelmExpression(HelmValue helmExpression, string key)
    {
        switch (helmExpression)
        {
            case { IsHelmSecretExpression: true, ValueContainsSecretValuesExpression: false }:
                Secrets[key] = helmExpression;
                return;
            case { IsHelmSecretExpression: false, ValueContainsSecretValuesExpression: false }:
                EnvironmentVariables[key] = helmExpression;
                break;
        }
    }

    private void ProcessEnvironmentStringValue(string stringValue, string key, string resourceName)
    {
        if (stringValue.ContainsHelmValuesSecretExpression())
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

    private async Task<object> ProcessValueAsync(KubernetesEnvironmentContext context, DistributedApplicationExecutionContext executionContext, object value, bool embedded = false)
    {
        while (true)
        {
            if (value is string s)
            {
                return s;
            }

            if (value is EndpointReference ep)
            {
                var referencedResource = ep.Resource == this
                    ? this
                    : await context.CreateKubernetesResourceAsync(ep.Resource, executionContext, default).ConfigureAwait(false);

                var mapping = referencedResource.EndpointMappings[ep.EndpointName];

                var url = GetEndpointValue(mapping, EndpointProperty.Url);

                return url;
            }

            if (value is ParameterResource param)
            {
                return AllocateParameter(param, TargetResource);
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
                var referencedResource = epExpr.Endpoint.Resource == this
                    ? this
                    : await context.CreateKubernetesResourceAsync(epExpr.Endpoint.Resource, executionContext, default).ConfigureAwait(false);

                var mapping = referencedResource.EndpointMappings[epExpr.Endpoint.EndpointName];

                var val = GetEndpointValue(mapping, epExpr.Property, embedded && epExpr.Property is EndpointProperty.Port or EndpointProperty.TargetPort);

                return val;
            }

            if (value is ReferenceExpression expr)
            {
                if (expr is { Format: "{0}", ValueProviders.Count: 1 })
                {
                    return (await ProcessValueAsync(context, executionContext, expr.ValueProviders[0], true).ConfigureAwait(false)).ToString() ?? string.Empty;
                }

                var args = new object[expr.ValueProviders.Count];
                var index = 0;

                foreach (var vp in expr.ValueProviders)
                {
                    var val = await ProcessValueAsync(context, executionContext, vp, true).ConfigureAwait(false);
                    args[index++] = val ?? throw new InvalidOperationException("Value is null");
                }

                return string.Format(CultureInfo.InvariantCulture, expr.Format, args);
            }

            // If we don't know how to process the value, we just return it as an external reference
            if (value is IManifestExpressionProvider r)
            {
                context.Logger.NotSupportedResourceWarning(nameof(value), r.GetType().Name);

                return ResolveUnknownValue(r, TargetResource);
            }

            throw new NotSupportedException($"Unsupported value type: {value.GetType().Name}");
        }
    }

    private static string GetEndpointValue(EndpointMapping mapping, EndpointProperty property, bool embedded = false)
    {
        var (scheme, _, host, port, _, _) = mapping;

        return property switch
        {
            EndpointProperty.Url => GetHostValue($"{scheme}://", suffix: GetPortSuffix()),
            EndpointProperty.Host or EndpointProperty.IPV4Host => GetHostValue(),
            EndpointProperty.Port => GetPort(),
            EndpointProperty.HostAndPort => GetHostValue(suffix: GetPortSuffix()),
            EndpointProperty.TargetPort => GetPort(),
            EndpointProperty.Scheme => scheme,
            _ => throw new NotSupportedException(),
        };

        string GetHostValue(string? prefix = null, string? suffix = null)
        {
            return $"{prefix}{host}{suffix}";
        }

        string GetPort()
        {
            var rawPort = embedded ? port.Expression ?? port.ValueString : port.ToScalar();

            return string.IsNullOrWhiteSpace(rawPort)
                ? string.Empty
                : rawPort;
        }

        string GetPortSuffix()
        {
            var portValue = port switch
            {
                _ when !string.IsNullOrWhiteSpace(port.Expression)
                  => port.Expression,
                { ValueString: { } } => port.ValueString,
                _ => null
            };

            return string.IsNullOrWhiteSpace(portValue)
                 ? string.Empty
                 : $":{portValue}";
        }
    }

    private static HelmValue AllocateParameter(ParameterResource parameter, IResource resource)
    {
        var formattedName = parameter.Name.ToHelmValuesSectionName();

        var expression = parameter.Secret ?
            formattedName.ToHelmSecretExpression(resource.Name) :
            formattedName.ToHelmConfigExpression(resource.Name);

        // Store the parameter itself for deferred resolution instead of resolving the value immediately
        if (parameter.Default is null || parameter.Secret)
        {
            return new(expression, (string?)null);
        }
        else
        {
            return new(expression, parameter);
        }
    }
    
    private static HelmValue ResolveUnknownValue(IManifestExpressionProvider parameter, IResource resource)
    {
        var formattedName = parameter.ValueExpression.Replace(HelmExtensions.StartDelimiter, string.Empty)
            .Replace(HelmExtensions.EndDelimiter, string.Empty)
            .Replace(".", "_")
            .ToHelmValuesSectionName();

        var helmExpression = parameter.ValueExpression.ContainsHelmValuesSecretExpression() ?
            formattedName.ToHelmSecretExpression(resource.Name) :
            formattedName.ToHelmConfigExpression(resource.Name);

        return new(helmExpression, parameter.ValueExpression);
    }

    private static string GetKubernetesProtocolName(ProtocolType type)
        => type switch
        {
            ProtocolType.Tcp => "TCP",
            ProtocolType.Udp => "UDP",
            _ => throw new InvalidOperationException($"Unsupported protocol type: {type}"),
        };

    internal class HelmValue
    {
        private HelmValue(object? value) : this(null, value) { }

        public HelmValue(string? expression, object? value)
        {
            Expression = expression;
            Value = value;
            ValueType = value?.GetType();
            ParameterSource = null;
        }

        public HelmValue(string expression, ParameterResource parameterSource)
        {
            Expression = expression;
            ParameterSource = parameterSource;
            Value = null!;
            ValueType = null!;
        }

        public string? Expression { get; }
        public object? Value { get; }
        protected Type? ValueType { get; }
        public string? ValueString => Value?.ToString();
        public ParameterResource? ParameterSource { get; }

        public bool IsHelmSecretExpression
            => Expression?.ContainsHelmValuesSecretExpression() ?? false;
        public bool ValueContainsSecretValuesExpression
            => ValueString?.ContainsHelmValuesSecretExpression() ?? false;
        public bool ValueContainsHelmExpression
            => ValueString?.ContainsHelmValuesExpression() ?? false;

        public override string ToString()
        {
            return ValueString ?? Expression ?? string.Empty;
        }

        public string ToScalar()
        {
            if (string.IsNullOrWhiteSpace(Expression))
            {
                return ValueString ?? string.Empty;
            }

            var expression = HelmExtensions.ScalarExpressionPattern().Match(Expression);
            if (!expression.Success)
            {
                return ToString();
            }
            
            var typeConversion = ValueType switch
            {
                var t when t == typeof(int) => $" {HelmExtensions.PipelineDelimiter} int",
                var t when t == typeof(long) => $" {HelmExtensions.PipelineDelimiter} int64",
                var t when t == typeof(float)
                        || t == typeof(double)
                        || t == typeof(decimal) => $" {HelmExtensions.PipelineDelimiter} float64",
                _ => string.Empty
            };

            return $"{expression.Captures[0].Value.Trim()}{typeConversion}".ToHelmExpression();
        }

        public static HelmValue Literal(object value) => new(value);

    }
}

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

        await ProcessEnvironmentAsync(context, executionContext, cancellationToken).ConfigureAwait(false);
        await ProcessArgumentsAsync(context, executionContext, cancellationToken).ConfigureAwait(false);

        BuildKubernetesResources();
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

        var paramName = $"port_{endpoint.Name}".ToHelmValuesSectionName();

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

    private async Task ProcessArgumentsAsync(KubernetesEnvironmentContext environmentContext, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
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
                var value = await this.ProcessValueAsync(environmentContext, executionContext, arg).ConfigureAwait(false);

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
                var key = environmentVariable.Key.ToHelmValuesSectionName();
                var value = await this.ProcessValueAsync(environmentContext, executionContext, environmentVariable.Value).ConfigureAwait(false);

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

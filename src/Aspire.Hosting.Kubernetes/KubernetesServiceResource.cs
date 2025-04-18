// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Kubernetes.Extensions;
using Aspire.Hosting.Kubernetes.Resources;

namespace Aspire.Hosting.Kubernetes;

/// <summary>
/// Represents a compute resource for Kubernetes with strongly-typed properties.
/// </summary>
public class KubernetesServiceResource(string name, IResource resource, KubernetesEnvironmentResource kubernetesEnvironmentResource) : Resource(name), IResourceWithParent<KubernetesEnvironmentResource>
{
    /// <inheritdoc/>
    public KubernetesEnvironmentResource Parent => kubernetesEnvironmentResource;

    /// <summary>
    /// Specifies the type of storage used for Kubernetes deployments.
    /// </summary>
    /// <remarks>
    /// This property determines the storage medium used for the application.
    /// Possible values include "emptyDir", "hostPath", "pvc"
    /// </remarks>
    public string StorageType { get; set; } = "emptyDir";

    /// <summary>
    /// Specifies the name of the storage class to be used for persistent volume claims in Kubernetes.
    /// This property allows customization of the storage class for specifying storage requirements
    /// such as performance, retention policies, and provisioning parameters.
    /// If set to null, the default storage class for the cluster will be used.
    /// </summary>
    public string? StorageClassName { get; set; }

    /// <summary>
    /// Gets or sets the default storage size for persistent volumes.
    /// </summary>
    public string StorageSize { get; set; } = "1Gi";

    /// <summary>
    /// Gets or sets the default access policy for reading and writing to the storage.
    /// </summary>
    public string StorageReadWritePolicy { get; set; } = "ReadWriteOnce";

    /// <summary>
    /// Gets or sets the policy that determines how Docker images are pulled during deployment.
    /// Possible values are:
    /// "Always" - Always attempt to pull the image from the registry.
    /// "IfNotPresent" - Pull the image only if it is not already present locally.
    /// "Never" - Never pull the image, use only the local image.
    /// The default value is "IfNotPresent".
    /// </summary>
    public string ImagePullPolicy { get; set; } = "IfNotPresent";

    /// <summary>
    /// Gets or sets the Kubernetes service type to be used when generating artifacts.
    /// </summary>
    /// <remarks>
    /// The default value is "ClusterIP". This property determines the type of service
    /// (e.g., ClusterIP, NodePort, LoadBalancer) created in Kubernetes for the application.
    /// </remarks>
    public string ServiceType { get; set; } = "ClusterIP";

    internal record EndpointMapping(string Scheme, string Host, string Port, string Name, string? HelmExpression = null);
    internal Dictionary<string, EndpointMapping> EndpointMappings { get; } = [];
    internal Dictionary<string, HelmExpressionWithValue> EnvironmentVariables { get; } = [];
    internal Dictionary<string, HelmExpressionWithValue> Secrets { get; } = [];
    internal Dictionary<string, HelmExpressionWithValue> Parameters { get; } = [];
    internal Dictionary<string, string> Labels { get; private set; } = [];
    internal List<BaseKubernetesResource> TemplatedResources { get; } = [];
    internal List<string> Commands { get; } = [];
    internal List<VolumeMountV1> Volumes { get; } = [];

    /// <summary>
    /// Gets the resource that is the target of this Docker Compose service.
    /// </summary>
    internal IResource TargetResource => resource;

    internal void BuildKubernetesResources()
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
                var key = environmentVariable.Key.ToManifestFriendlyResourceName();
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

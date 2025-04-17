// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppContainers;
using Aspire.Hosting.Lifecycle;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents the infrastructure for Azure Container Apps within the Aspire Hosting environment.
/// Implements the <see cref="IDistributedApplicationLifecycleHook"/> interface to provide lifecycle hooks for distributed applications.
/// </summary>
internal sealed class AzureContainerAppsInfrastructure(
    ILogger<AzureContainerAppsInfrastructure> logger,
    IOptions<AzureProvisioningOptions> provisioningOptions,
    DistributedApplicationExecutionContext executionContext) : IDistributedApplicationLifecycleHook
{
    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (executionContext.IsRunMode)
        {
            return;
        }

        // TODO: We need to support direct association between a compute resource and the container app environment.
        // Right now we support a single container app environment as the one we want to use and we'll fall back to
        // azd based environment if we don't have one.

        var caes = appModel.Resources.OfType<AzureContainerAppEnvironmentResource>().ToArray();

        if (caes.Length > 1)
        {
            throw new NotSupportedException("Multiple container app environments are not supported.");
        }

        var environment = caes.FirstOrDefault() as IAzureContainerAppEnvironment ?? new AzdAzureContainerAppEnvironment();

        var containerAppEnvironmentContext = new ContainerAppEnvironmentContext(
            logger,
            environment);

        foreach (var r in appModel.Resources)
        {
            if (r.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var lastAnnotation) && lastAnnotation == ManifestPublishingCallbackAnnotation.Ignore)
            {
                continue;
            }

            if (!r.IsContainer() && r is not ProjectResource)
            {
                continue;
            }

            var containerApp = await containerAppEnvironmentContext.CreateContainerAppAsync(r, provisioningOptions.Value, executionContext, cancellationToken).ConfigureAwait(false);

            // Capture information about the container registry used by the
            // container app environment in the deployment target information
            // associated with each compute resource that needs an image
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            r.Annotations.Add(new DeploymentTargetAnnotation(containerApp)
            {
                ContainerRegistryInfo = caes.FirstOrDefault(),
                ComputeEnvironment = environment as IComputeEnvironmentResource // will be null if azd
            });
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        }

        static void SetKnownParameterValue(AzureBicepResource r, string key, Func<AzureBicepResource, object> factory)
        {
            if (r.Parameters.TryGetValue(key, out var existingValue) && existingValue is null)
            {
                var value = factory(r);

                r.Parameters[key] = value;
            }
        }

        if (environment is AzdAzureContainerAppEnvironment)
        {
            // We avoid setting known values if azd is used, it will be resolved by azd at publish time.
            return;
        }

        // Resolve the known parameters for the container app environment
        foreach (var r in appModel.Resources.OfType<AzureBicepResource>())
        {
            // HACK: This forces parameters to be resolved for any AzureProvisioningResource
            r.GetBicepTemplateFile();

            // This will throw an exception if there's no value specified, in this new mode, we don't no longer support
            // automagic secret key vault references.
            SetKnownParameterValue(r, AzureBicepResource.KnownParameters.KeyVaultName, environment.GetSecretOutputKeyVault);

            // Set the known parameters for the container app environment
            SetKnownParameterValue(r, AzureBicepResource.KnownParameters.PrincipalId, _ => environment.PrincipalId);
            SetKnownParameterValue(r, AzureBicepResource.KnownParameters.PrincipalType, _ => "ServicePrincipal");
            SetKnownParameterValue(r, AzureBicepResource.KnownParameters.PrincipalName, _ => environment.PrincipalName);
            SetKnownParameterValue(r, AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId, _ => environment.LogAnalyticsWorkspaceId);

            SetKnownParameterValue(r, "containerAppEnvironmentId", _ => environment.ContainerAppEnvironmentId);
            SetKnownParameterValue(r, "containerAppEnvironmentName", _ => environment.ContainerAppEnvironmentName);
        }
    }

    private sealed class ContainerAppEnvironmentContext(
        ILogger logger,
        IAzureContainerAppEnvironment environment
        )
    {
        private ILogger Logger => logger;
        public IAzureContainerAppEnvironment Environment => environment;

        private readonly Dictionary<IResource, ContainerAppContext> _containerApps = [];

        public async Task<AzureBicepResource> CreateContainerAppAsync(IResource resource, AzureProvisioningOptions provisioningOptions, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            var context = await ProcessResourceAsync(resource, executionContext, cancellationToken).ConfigureAwait(false);

            var provisioningResource = new AzureProvisioningResource(resource.Name, context.BuildContainerApp)
            {
                ProvisioningBuildOptions = provisioningOptions.ProvisioningBuildOptions
            };

            return provisioningResource;
        }

        private async Task<ContainerAppContext> ProcessResourceAsync(IResource resource, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            if (!_containerApps.TryGetValue(resource, out var context))
            {
                _containerApps[resource] = context = new ContainerAppContext(resource, this);
                await context.ProcessResourceAsync(executionContext, cancellationToken).ConfigureAwait(false);
            }

            return context;
        }

        private sealed class ContainerAppContext(IResource resource, ContainerAppEnvironmentContext containerAppEnvironmentContext)
        {
            private readonly Dictionary<object, string> _allocatedParameters = [];
            private readonly Dictionary<string, ProvisioningParameter> _provisioningParameters = [];
            private readonly ContainerAppEnvironmentContext _containerAppEnvironmentContext = containerAppEnvironmentContext;

            record struct EndpointMapping(string Scheme, string Host, int Port, int? TargetPort, bool IsHttpIngress, bool External);

            private readonly Dictionary<string, EndpointMapping> _endpointMapping = [];

            private (int? Port, bool Http2, bool External)? _httpIngress;
            private readonly List<int> _additionalPorts = [];

            private ProvisioningParameter? _containerRegistryUrlParameter;
            private ProvisioningParameter? _containerRegistryManagedIdentityIdParameter;

            public IResource Resource => resource;

            // Set the parameters to add to the bicep file
            public Dictionary<string, IManifestExpressionProvider> Parameters { get; } = [];

            public List<ContainerAppEnvironmentVariable> EnvironmentVariables { get; } = [];

            public List<ContainerAppWritableSecret> Secrets { get; } = [];

            public List<BicepValue<string>> Args { get; } = [];

            public List<(ContainerAppVolume, ContainerAppVolumeMount)> Volumes { get; } = [];

            public Dictionary<string, KeyVaultService> KeyVaultRefs { get; } = [];
            public Dictionary<string, KeyVaultSecret> KeyVaultSecretRefs { get; } = [];

            public void BuildContainerApp(AzureResourceInfrastructure c)
            {
                // Write a fake parameter for the container app environment
                // so azd knows the Dashboard URL - see https://github.com/dotnet/aspire/issues/8449.
                // This is temporary until a real fix can be made in azd.
                AllocateParameter(_containerAppEnvironmentContext.Environment.ContainerAppDomain);

                var containerAppIdParam = AllocateParameter(_containerAppEnvironmentContext.Environment.ContainerAppEnvironmentId);

                ProvisioningParameter? containerImageParam = null;

                if (!TryGetContainerImageName(resource, out var containerImageName))
                {
                    AllocateContainerRegistryParameters();

                    containerImageParam = AllocateContainerImageParameter();
                }

                var containerAppResource = new ContainerApp(Infrastructure.NormalizeBicepIdentifier(resource.Name))
                {
                    Name = resource.Name.ToLowerInvariant()
                };

                BicepValue<string>? containerAppIdentityId = null;

                if (resource.TryGetLastAnnotation<AppIdentityAnnotation>(out var appIdentityAnnotation))
                {
                    var appIdentityResource = appIdentityAnnotation.IdentityResource;

                    containerAppIdentityId = appIdentityResource.Id.AsProvisioningParameter(c);

                    var id = BicepFunction.Interpolate($"{containerAppIdentityId}").Compile().ToString();

                    containerAppResource.Identity.ManagedServiceIdentityType = ManagedServiceIdentityType.UserAssigned;
                    containerAppResource.Identity.UserAssignedIdentities[id] = new UserAssignedIdentityDetails();

                    EnvironmentVariables.Add(new ContainerAppEnvironmentVariable { Name = "AZURE_CLIENT_ID", Value = appIdentityResource.ClientId.AsProvisioningParameter(c) });
                }

                AddContainerRegistryManagedIdentity(containerAppResource);

                containerAppResource.EnvironmentId = containerAppIdParam;

                var configuration = new ContainerAppConfiguration()
                {
                    ActiveRevisionsMode = ContainerAppActiveRevisionsMode.Single,
                };
                containerAppResource.Configuration = configuration;

                AddIngress(configuration);

                AddContainerRegistryParameters(configuration);
                AddSecrets(containerAppIdentityId, configuration);

                var template = new ContainerAppTemplate();
                containerAppResource.Template = template;

                foreach (var (volume, _) in Volumes)
                {
                    template.Volumes.Add(volume);
                }

                template.Scale = new ContainerAppScale()
                {
                    MinReplicas = resource.GetReplicaCount()
                };

                var containerAppContainer = new ContainerAppContainer();
                template.Containers = [containerAppContainer];

                containerAppContainer.Image = containerImageParam is null ? containerImageName! : containerImageParam;
                containerAppContainer.Name = resource.Name;

                SetEntryPoint(containerAppContainer);
                AddEnvironmentVariablesAndCommandLineArgs(containerAppContainer);

                foreach (var (_, mountedVolume) in Volumes)
                {
                    containerAppContainer.VolumeMounts.Add(mountedVolume);
                }

                // Add parameters to the provisioningResource
                foreach (var (_, parameter) in _provisioningParameters)
                {
                    c.Add(parameter);
                }

                // Keyvault
                foreach (var (_, v) in KeyVaultRefs)
                {
                    c.Add(v);
                }

                foreach (var (_, v) in KeyVaultSecretRefs)
                {
                    c.Add(v);
                }

                c.Add(containerAppResource);

                // Write the parameters we generated to the construct so they are included in the manifest
                foreach (var (key, value) in Parameters)
                {
                    c.AspireResource.Parameters[key] = value;
                }

                if (resource.TryGetAnnotationsOfType<AzureContainerAppCustomizationAnnotation>(out var annotations))
                {
                    foreach (var a in annotations)
                    {
                        a.Configure(c, containerAppResource);
                    }
                }
            }

            private static bool TryGetContainerImageName(IResource resource, out string? containerImageName)
            {
                // If the resource has a Dockerfile build annotation, we don't have the image name
                // it will come as a parameter
                if (resource.TryGetLastAnnotation<DockerfileBuildAnnotation>(out _))
                {
                    containerImageName = null;
                    return false;
                }

                return resource.TryGetContainerImageName(out containerImageName);
            }

            public async Task ProcessResourceAsync(DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
            {
                ProcessEndpoints();
                ProcessVolumes();

                await ProcessEnvironmentAsync(executionContext, cancellationToken).ConfigureAwait(false);
                await ProcessArgumentsAsync(executionContext, cancellationToken).ConfigureAwait(false);
            }

            private void ProcessEndpoints()
            {
                if (!resource.TryGetEndpoints(out var endpoints) || !endpoints.Any())
                {
                    return;
                }

                // Only http, https, and tcp are supported
                var unsupportedEndpoints = endpoints.Where(e => e.UriScheme is not ("tcp" or "http" or "https")).ToArray();

                if (unsupportedEndpoints.Length > 0)
                {
                    throw new NotSupportedException($"The endpoint(s) {string.Join(", ", unsupportedEndpoints.Select(e => $"'{e.Name}'"))} specify an unsupported scheme. The supported schemes are 'http', 'https', and 'tcp'.");
                }

                // We can allocate ports per endpoint
                var portAllocator = new PortAllocator();

                var endpointIndexMap = new Dictionary<string, int>();

                // This is used to determine if an endpoint should be treated as the Default endpoint.
                // Endpoints can come from 3 different sources (in this order):
                // 1. Kestrel configuration
                // 2. Default endpoints added by the framework
                // 3. Explicitly added endpoints
                // But wherever they come from, we treat the first one as Default, for each scheme.
                var httpSchemesEncountered = new HashSet<string>();

                static bool IsHttpScheme(string scheme) => scheme is "http" or "https";

                // Allocate ports for the endpoints
                foreach (var endpoint in endpoints)
                {
                    endpointIndexMap[endpoint.Name] = endpointIndexMap.Count;

                    int? targetPort = (resource, endpoint.UriScheme, endpoint.TargetPort, endpoint.Port) switch
                    {
                        // The port was specified so use it
                        (_, _, int target, _) => target,

                        // Container resources get their default listening port from the exposed port.
                        (ContainerResource, _, null, int port) => port,

                        // Check whether the project view this endpoint as Default (for its scheme).
                        // If so, we don't specify the target port, as it will get one from the deployment tool.
                        (ProjectResource project, string uriScheme, null, _) when IsHttpScheme(uriScheme) && !httpSchemesEncountered.Contains(uriScheme) => null,

                        // Allocate a dynamic port
                        _ => portAllocator.AllocatePort()
                    };

                    // We only keep track of schemes for project resources, since we don't want
                    // a non-project scheme to affect what project endpoints are considered default.
                    if (resource is ProjectResource && IsHttpScheme(endpoint.UriScheme))
                    {
                        httpSchemesEncountered.Add(endpoint.UriScheme);
                    }

                    int? exposedPort = (endpoint.UriScheme, endpoint.Port, targetPort) switch
                    {
                        // Exposed port and target port are the same, we don't need to mention the exposed port
                        (_, int p0, int p1) when p0 == p1 => null,

                        // Port was specified, so use it
                        (_, int port, _) => port,

                        // We have a target port, not need to specify an exposedPort
                        // it will default to the targetPort
                        (_, null, int port) => null,

                        // Let the tool infer the default http and https ports
                        ("http", null, null) => null,
                        ("https", null, null) => null,

                        // Other schemes just allocate a port
                        _ => portAllocator.AllocatePort()
                    };

                    if (exposedPort is int ep)
                    {
                        portAllocator.AddUsedPort(ep);
                        endpoint.Port = ep;
                    }

                    if (targetPort is int tp)
                    {
                        portAllocator.AddUsedPort(tp);
                        endpoint.TargetPort = tp;
                    }
                }

                // First we group the endpoints by container port (aka destinations), this gives us the logical bindings or destinations
                var endpointsByTargetPort = endpoints.GroupBy(e => e.TargetPort)
                                                     .Select(g => new
                                                     {
                                                         Port = g.Key,
                                                         Endpoints = g.ToArray(),
                                                         External = g.Any(e => e.IsExternal),
                                                         IsHttpOnly = g.All(e => e.UriScheme is "http" or "https"),
                                                         AnyH2 = g.Any(e => e.Transport is "http2"),
                                                         UniqueSchemes = g.Select(e => e.UriScheme).Distinct().ToArray(),
                                                         Index = g.Min(e => endpointIndexMap[e.Name])
                                                     })
                                                     .ToList();

                // Failure cases

                // Multiple external endpoints are not supported
                if (endpointsByTargetPort.Count(g => g.External) > 1)
                {
                    throw new NotSupportedException("Multiple external endpoints are not supported");
                }

                // Any external non-http endpoints are not supported
                if (endpointsByTargetPort.Any(g => g.External && !g.IsHttpOnly))
                {
                    throw new NotSupportedException("External non-HTTP(s) endpoints are not supported");
                }

                // Don't allow mixing http and tcp endpoints
                // This means we want to fail if we see a group with http/https and tcp endpoints
                static bool Compatible(string[] schemes) =>
                    schemes.All(s => s is "http" or "https") || schemes.All(s => s is "tcp");

                if (endpointsByTargetPort.Any(g => !Compatible(g.UniqueSchemes)))
                {
                    throw new NotSupportedException("HTTP(s) and TCP endpoints cannot be mixed");
                }

                // Get all http only groups
                var httpOnlyEndpoints = endpointsByTargetPort.Where(g => g.IsHttpOnly).OrderBy(g => g.Index).ToArray();

                // Do we only have one?
                var httpIngress = httpOnlyEndpoints.Length == 1 ? httpOnlyEndpoints[0] : null;

                if (httpIngress is null)
                {
                    // We have more than one, pick prefer external one
                    var externalHttp = httpOnlyEndpoints.Where(g => g.External).ToArray();

                    if (externalHttp.Length == 1)
                    {
                        httpIngress = externalHttp[0];
                    }
                    else if (httpOnlyEndpoints.Length > 0)
                    {
                        httpIngress = httpOnlyEndpoints[0];
                    }
                }

                if (httpIngress is not null)
                {
                    // We're processed the http ingress, remove it from the list
                    endpointsByTargetPort.Remove(httpIngress);

                    var targetPort = httpIngress.Port ?? (resource is ProjectResource ? null : 80);

                    _httpIngress = (targetPort, httpIngress.AnyH2, httpIngress.External);

                    foreach (var e in httpIngress.Endpoints)
                    {
                        if (e.UriScheme is "http" && e.Port is not null and not 80)
                        {
                            throw new NotSupportedException($"The endpoint '{e.Name}' is an http endpoint and must use port 80");
                        }

                        if (e.UriScheme is "https" && e.Port is not null and not 443)
                        {
                            throw new NotSupportedException($"The endpoint '{e.Name}' is an https endpoint and must use port 443");
                        }

                        // For the http ingress port is always 80 or 443
                        var port = e.UriScheme is "http" ? 80 : 443;

                        _endpointMapping[e.Name] = new(e.UriScheme, resource.Name, port, targetPort, true, httpIngress.External);
                    }
                }

                if (endpointsByTargetPort.Count > 5)
                {
                    _containerAppEnvironmentContext.Logger.LogWarning("More than 5 additional ports are not supported. See https://learn.microsoft.com/en-us/azure/container-apps/ingress-overview#tcp for more details.");
                }

                foreach (var g in endpointsByTargetPort)
                {
                    if (g.Port is null)
                    {
                        throw new NotSupportedException("Container port is required for all endpoints");
                    }

                    _additionalPorts.Add(g.Port.Value);

                    foreach (var e in g.Endpoints)
                    {
                        _endpointMapping[e.Name] = new(e.UriScheme, resource.Name, e.Port ?? g.Port.Value, g.Port.Value, false, g.External);
                    }
                }
            }

            private async Task ProcessArgumentsAsync(DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
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
                        var (val, _) = await ProcessValueAsync(arg, executionContext, cancellationToken).ConfigureAwait(false);

                        var argValue = ResolveValue(val);

                        Args.Add(argValue);
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

                    foreach (var kv in context.EnvironmentVariables)
                    {
                        var (val, secretType) = await ProcessValueAsync(kv.Value, executionContext, cancellationToken).ConfigureAwait(false);

                        var argValue = ResolveValue(val);

                        if (secretType != SecretType.None)
                        {
                            var secretName = kv.Key.Replace("_", "-").ToLowerInvariant();

                            var secret = new ContainerAppWritableSecret()
                            {
                                Name = secretName
                            };

                            if (secretType == SecretType.KeyVault)
                            {
                                // TODO: this should be able to use ToUri(), but it hit an issue
                                secret.KeyVaultUri = new BicepValue<Uri>(((BicepExpression?)argValue)!);
                            }
                            else
                            {
                                secret.Value = argValue;
                            }

                            Secrets.Add(secret);

                            // The value is the secret name
                            val = secretName;
                        }

                        EnvironmentVariables.Add(secretType switch
                        {
                            SecretType.None => new ContainerAppEnvironmentVariable { Name = kv.Key, Value = argValue },
                            SecretType.Normal or SecretType.KeyVault => new ContainerAppEnvironmentVariable { Name = kv.Key, SecretRef = (string)val },
                            _ => throw new NotSupportedException()
                        });
                    }
                }
            }

            private static BicepValue<string> ResolveValue(object val)
            {
                return val switch
                {
                    BicepValue<string> s => s,
                    string s => s,
                    ProvisioningParameter p => p,
                    BicepFormatString fs => BicepFunction2.Interpolate(fs),
                    _ => throw new NotSupportedException("Unsupported value type " + val.GetType())
                };
            }

            private void ProcessVolumes()
            {
                if (resource.TryGetContainerMounts(out var mounts))
                {
                    var bindMountIndex = 0;
                    var volumeIndex = 0;

                    foreach (var volume in mounts)
                    {
                        var (index, volumeName) = volume.Type switch
                        {
                            ContainerMountType.BindMount => (bindMountIndex, $"bm{bindMountIndex}"),
                            ContainerMountType.Volume => (volumeIndex, $"v{volumeIndex}"),
                            _ => throw new NotSupportedException()
                        };

                        if (volume.Type == ContainerMountType.BindMount)
                        {
                            bindMountIndex++;
                        }
                        else
                        {
                            volumeIndex++;
                        }

                        var volumeStorageParameter = AllocateVolumeStorageAccount(volume, index);

                        var containerAppVolume = new ContainerAppVolume
                        {
                            Name = volumeName,
                            StorageType = ContainerAppStorageType.AzureFile,
                            StorageName = volumeStorageParameter,
                        };

                        var containerAppVolumeMount = new ContainerAppVolumeMount
                        {
                            VolumeName = volumeName,
                            MountPath = volume.Target,
                        };

                        Volumes.Add((containerAppVolume, containerAppVolumeMount));
                    }
                }
            }

            private BicepValue<string> GetValue(EndpointMapping mapping, EndpointProperty property)
            {
                var (scheme, host, port, targetPort, isHttpIngress, external) = mapping;

                BicepValue<string> GetHostValue(string? prefix = null, string? suffix = null)
                {
                    if (isHttpIngress)
                    {
                        var domain = AllocateParameter(_containerAppEnvironmentContext.Environment.ContainerAppDomain);

                        return external ? BicepFunction.Interpolate($$"""{{prefix}}{{host}}.{{domain}}{{suffix}}""") : BicepFunction.Interpolate($$"""{{prefix}}{{host}}.internal.{{domain}}{{suffix}}""");
                    }

                    return $"{prefix}{host}{suffix}";
                }

                return property switch
                {
                    EndpointProperty.Url => GetHostValue($"{scheme}://", suffix: isHttpIngress ? null : $":{port}"),
                    EndpointProperty.Host or EndpointProperty.IPV4Host => GetHostValue(),
                    EndpointProperty.Port => port.ToString(CultureInfo.InvariantCulture),
                    EndpointProperty.HostAndPort => GetHostValue(suffix: $":{port}"),
                    EndpointProperty.TargetPort => targetPort is null ? AllocateContainerPortParameter() : targetPort,
                    EndpointProperty.Scheme => scheme,
                    _ => throw new NotSupportedException(),
                };
            }

            private async Task<(object, SecretType)> ProcessValueAsync(object value, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken, SecretType secretType = SecretType.None, object? parent = null)
            {
                if (value is string s)
                {
                    return (s, secretType);
                }

                if (value is EndpointReference ep)
                {
                    var context = ep.Resource == resource
                        ? this
                        : await _containerAppEnvironmentContext.ProcessResourceAsync(ep.Resource, executionContext, cancellationToken).ConfigureAwait(false);

                    var mapping = context._endpointMapping[ep.EndpointName];

                    var url = GetValue(mapping, EndpointProperty.Url);

                    return (url, secretType);
                }

                if (value is ParameterResource param)
                {
                    var st = param.Secret ? SecretType.Normal : secretType;

                    return (AllocateParameter(param, secretType: st), st);
                }

                if (value is ConnectionStringReference cs)
                {
                    return await ProcessValueAsync(cs.Resource.ConnectionStringExpression, executionContext, cancellationToken, secretType: secretType, parent: parent).ConfigureAwait(false);
                }

                if (value is IResourceWithConnectionString csrs)
                {
                    return await ProcessValueAsync(csrs.ConnectionStringExpression, executionContext, cancellationToken, secretType: secretType, parent: parent).ConfigureAwait(false);
                }

                if (value is BicepOutputReference output)
                {
                    return (AllocateParameter(output, secretType: secretType), secretType);
                }

                if (value is BicepSecretOutputReference secretOutputReference)
                {
                    if (parent is null)
                    {
                        return (AllocateKeyVaultSecretUriReference(secretOutputReference), SecretType.KeyVault);
                    }

                    return (AllocateParameter(secretOutputReference, secretType: SecretType.KeyVault), SecretType.KeyVault);
                }

                if (value is IAzureKeyVaultSecretReference vaultSecretReference)
                {
                    if (parent is null)
                    {
                        return (AllocateKeyVaultSecretUriReference(vaultSecretReference), SecretType.KeyVault);
                    }

                    return (AllocateParameter(vaultSecretReference, secretType: SecretType.KeyVault), SecretType.KeyVault);
                }

                if (value is EndpointReferenceExpression epExpr)
                {
                    var context = epExpr.Endpoint.Resource == resource
                        ? this
                        : await _containerAppEnvironmentContext.ProcessResourceAsync(epExpr.Endpoint.Resource, executionContext, cancellationToken).ConfigureAwait(false);

                    var mapping = context._endpointMapping[epExpr.Endpoint.EndpointName];

                    var val = GetValue(mapping, epExpr.Property);

                    return (val, secretType);
                }

                if (value is ReferenceExpression expr)
                {
                    // Special case simple expressions
                    if (expr.Format == "{0}" && expr.ValueProviders.Count == 1)
                    {
                        return await ProcessValueAsync(expr.ValueProviders[0], executionContext, cancellationToken, secretType, parent: parent).ConfigureAwait(false);
                    }

                    var args = new object[expr.ValueProviders.Count];
                    var index = 0;
                    var finalSecretType = SecretType.None;

                    foreach (var vp in expr.ValueProviders)
                    {
                        var (val, secret) = await ProcessValueAsync(vp, executionContext, cancellationToken, secretType, parent: expr).ConfigureAwait(false);

                        if (secret != SecretType.None)
                        {
                            finalSecretType = SecretType.Normal;
                        }

                        args[index++] = val;
                    }

                    return (new BicepFormatString(expr.Format, args), finalSecretType);

                }

                throw new NotSupportedException("Unsupported value type " + value.GetType());
            }

            private ProvisioningParameter AllocateVolumeStorageAccount(ContainerMountAnnotation volume, int volumeIndex) =>
                AllocateParameter(_containerAppEnvironmentContext.Environment.GetVolumeStorage(resource, volume, volumeIndex));

            private BicepValue<string> AllocateKeyVaultSecretUriReference(BicepSecretOutputReference secretOutputReference)
            {
                if (!KeyVaultRefs.TryGetValue(secretOutputReference.Resource.Name, out var kv))
                {
                    // We resolve the keyvault that represents the storage for secret outputs
                    var parameter = AllocateParameter(_containerAppEnvironmentContext.Environment.GetSecretOutputKeyVault(secretOutputReference.Resource));
                    kv = KeyVaultService.FromExisting($"{parameter.BicepIdentifier}_kv");
                    kv.Name = parameter;

                    KeyVaultRefs[secretOutputReference.Resource.Name] = kv;
                }

                if (!KeyVaultSecretRefs.TryGetValue(secretOutputReference.ValueExpression, out var secret))
                {
                    // Now we resolve the secret
                    var secretBicepIdentifier = Infrastructure.NormalizeBicepIdentifier($"{kv.BicepIdentifier}_{secretOutputReference.Name}");
                    secret = KeyVaultSecret.FromExisting(secretBicepIdentifier);
                    secret.Name = secretOutputReference.Name;
                    secret.Parent = kv;

                    KeyVaultSecretRefs[secretOutputReference.ValueExpression] = secret;
                }

                return secret.Properties.SecretUri;
            }

            private BicepValue<string> AllocateKeyVaultSecretUriReference(IAzureKeyVaultSecretReference secretOutputReference)
            {
                if (!KeyVaultRefs.TryGetValue(secretOutputReference.Resource.Name, out var kv))
                {
                    // We resolve the keyvault that represents the storage for secret outputs
                    var parameter = AllocateParameter(secretOutputReference.Resource.NameOutputReference);
                    kv = KeyVaultService.FromExisting($"{parameter.BicepIdentifier}_kv");
                    kv.Name = parameter;

                    KeyVaultRefs[secretOutputReference.Resource.Name] = kv;
                }

                if (!KeyVaultSecretRefs.TryGetValue(secretOutputReference.ValueExpression, out var secret))
                {
                    // Now we resolve the secret
                    var secretBicepIdentifier = Infrastructure.NormalizeBicepIdentifier($"{kv.BicepIdentifier}_{secretOutputReference.SecretName}");
                    secret = KeyVaultSecret.FromExisting(secretBicepIdentifier);
                    secret.Name = secretOutputReference.SecretName;
                    secret.Parent = kv;

                    KeyVaultSecretRefs[secretOutputReference.ValueExpression] = secret;
                }

                return secret.Properties.SecretUri;
            }

            private ProvisioningParameter AllocateContainerImageParameter()
                => AllocateParameter(ResourceExpression.GetContainerImageExpression(resource));

            private BicepValue<int> AllocateContainerPortParameter()
                => AllocateParameter(ResourceExpression.GetContainerPortExpression(resource));

            private void AllocateContainerRegistryParameters()
            {
                _containerRegistryUrlParameter ??= AllocateParameter(_containerAppEnvironmentContext.Environment.ContainerRegistryUrl);
                _containerRegistryManagedIdentityIdParameter ??= AllocateParameter(_containerAppEnvironmentContext.Environment.ContainerRegistryManagedIdentityId);
            }

            private ProvisioningParameter AllocateParameter(IManifestExpressionProvider parameter, Type? type = null, SecretType secretType = SecretType.None)
            {
                if (!_allocatedParameters.TryGetValue(parameter, out var parameterName))
                {
                    parameterName = parameter.ValueExpression.Replace("{", "").Replace("}", "").Replace(".", "_").Replace("-", "_").ToLowerInvariant();

                    if (parameterName[0] == '_')
                    {
                        parameterName = parameterName[1..];
                    }

                    _allocatedParameters[parameter] = parameterName;
                }

                if (!_provisioningParameters.TryGetValue(parameterName, out var provisioningParameter))
                {
                    _provisioningParameters[parameterName] = provisioningParameter = new ProvisioningParameter(parameterName, type ?? typeof(string)) { IsSecure = secretType != SecretType.None };
                }

                Parameters[parameterName] = parameter;
                return provisioningParameter;
            }

            private void AddIngress(ContainerAppConfiguration config)
            {
                if (_httpIngress is null && _additionalPorts.Count == 0)
                {
                    return;
                }

                // Now we map the remaining endpoints. These should be internal only tcp/http based endpoints
                var skipAdditionalPort = 0;

                var caIngress = new ContainerAppIngressConfiguration();

                if (_httpIngress is { } ingress)
                {
                    caIngress.External = ingress.External;
                    caIngress.TargetPort = ingress.Port ?? AllocateContainerPortParameter();
                    caIngress.Transport = ingress.Http2 ? ContainerAppIngressTransportMethod.Http2 : ContainerAppIngressTransportMethod.Http;
                }
                else if (_additionalPorts.Count > 0)
                {
                    // First port is the default
                    var port = _additionalPorts[0];

                    skipAdditionalPort++;

                    caIngress.External = false;
                    caIngress.TargetPort = port;
                    caIngress.Transport = ContainerAppIngressTransportMethod.Tcp;
                }

                // Add additional ports
                // https://learn.microsoft.com/en-us/azure/container-apps/ingress-how-to?pivots=azure-cli#use-additional-tcp-ports
                var additionalPorts = _additionalPorts.Skip(skipAdditionalPort);
                if (additionalPorts.Any())
                {
                    foreach (var port in additionalPorts)
                    {
                        caIngress.AdditionalPortMappings.Add(new IngressPortMapping
                        {
                            External = false,
                            TargetPort = port
                        });
                    }
                }

                config.Ingress = caIngress;
            }

            private void SetEntryPoint(ContainerAppContainer container)
            {
                if (resource is ContainerResource containerResource && containerResource.Entrypoint is { } entrypoint)
                {
                    container.Command = [entrypoint];
                }
            }

            private void AddEnvironmentVariablesAndCommandLineArgs(ContainerAppContainer container)
            {
                if (EnvironmentVariables.Count > 0)
                {
                    container.Env = [];

                    foreach (var ev in EnvironmentVariables)
                    {
                        container.Env.Add(ev);
                    }
                }

                if (Args.Count > 0)
                {
                    container.Args = new(Args);
                }
            }

            private void AddSecrets(BicepValue<string>? containerAppIdentityId, ContainerAppConfiguration config)
            {
                if (Secrets.Count == 0)
                {
                    return;
                }

                config.Secrets = [];

                foreach (var s in Secrets)
                {
                    IBicepValue keyVaultUri = s.KeyVaultUri;

                    if (keyVaultUri.Kind != BicepValueKind.Unset && containerAppIdentityId is not null)
                    {
                        s.Identity = containerAppIdentityId;
                    }

                    config.Secrets.Add(s);
                }
            }

            private void AddContainerRegistryManagedIdentity(ContainerApp app)
            {
                if (_containerRegistryManagedIdentityIdParameter is null)
                {
                    return;
                }

                // REVIEW: This is is a little hacky, we should probably have a better way to do this
                var id = BicepFunction.Interpolate($"{_containerRegistryManagedIdentityIdParameter}").Compile().ToString();

                app.Identity.ManagedServiceIdentityType = ManagedServiceIdentityType.UserAssigned;
                app.Identity.UserAssignedIdentities[id] = new UserAssignedIdentityDetails();
            }

            private void AddContainerRegistryParameters(ContainerAppConfiguration app)
            {
                if (_containerRegistryUrlParameter is null || _containerRegistryManagedIdentityIdParameter is null)
                {
                    return;
                }

                app.Registries = [
                    new ContainerAppRegistryCredentials
                    {
                        Server = _containerRegistryUrlParameter,
                        Identity = _containerRegistryManagedIdentityIdParameter
                    }
                ];
            }
        }
    }

    private sealed class ResourceExpression(IResource resource, string propertyExpression) : IManifestExpressionProvider
    {
        public string ValueExpression => $"{{{resource.Name}.{propertyExpression}}}";

        public static IManifestExpressionProvider GetContainerImageExpression(IResource p) =>
            new ResourceExpression(p, "containerImage");

        public static IManifestExpressionProvider GetContainerPortExpression(IResource p) =>
            new ResourceExpression(p, "containerPort");
    }

    private sealed class PortAllocator(int startPort = 8000)
    {
        private int _allocatedPortStart = startPort;
        private readonly HashSet<int> _usedPorts = [];

        public int AllocatePort()
        {
            while (_usedPorts.Contains(_allocatedPortStart))
            {
                _allocatedPortStart++;
            }

            return _allocatedPortStart;
        }

        public void AddUsedPort(int port)
        {
            _usedPorts.Add(port);
        }
    }

    enum SecretType
    {
        None,
        Normal,
        KeyVault,
    }
}

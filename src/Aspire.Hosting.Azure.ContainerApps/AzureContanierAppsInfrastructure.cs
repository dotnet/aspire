#pragma warning disable AZPROVISION001
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Resources;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents the infrastructure for Azure Container Apps within the Aspire Hosting environment.
/// Implements the <see cref="IDistributedApplicationLifecycleHook"/> interface to provide lifecycle hooks for distributed applications.
/// </summary>
internal sealed class AzureContainerAppsInfastructure(ILogger<AzureContainerAppsInfastructure> logger, DistributedApplicationExecutionContext executionContext) : IDistributedApplicationLifecycleHook
{
    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (executionContext.IsRunMode)
        {
            return;
        }

        var containerAppEnviromentContext = new ContainerAppEnviromentContext(
            logger,
            AzureContainerAppsEnvironment.AZURE_CONTAINER_APPS_ENVIRONMENT_ID,
            AzureContainerAppsEnvironment.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN,
            AzureContainerAppsEnvironment.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID,
            AzureContainerAppsEnvironment.AZURE_CONTAINER_REGISTRY_ENDPOINT,
            AzureContainerAppsEnvironment.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID,
            AzureContainerAppsEnvironment.MANAGED_IDENTITY_CLIENT_ID);

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

            var containerApp = await containerAppEnviromentContext.CreateContainerAppAsync(r, executionContext, cancellationToken).ConfigureAwait(false);

            r.Annotations.Add(new DeploymentTargetAnnotation(containerApp));
        }
    }

    private sealed class ContainerAppEnviromentContext(
        ILogger logger,
        IManifestExpressionProvider containerAppEnvironmentId,
        IManifestExpressionProvider containerAppDomain,
        IManifestExpressionProvider managedIdentityId,
        IManifestExpressionProvider containerRegistryUrl,
        IManifestExpressionProvider containerRegistryManagedIdentityId,
        IManifestExpressionProvider clientId
        )
    {
        private ILogger Logger => logger;
        private IManifestExpressionProvider ContainerAppEnvironmentId => containerAppEnvironmentId;
        private IManifestExpressionProvider ContainerAppDomain => containerAppDomain;
        private IManifestExpressionProvider ManagedIdentityId => managedIdentityId;
        private IManifestExpressionProvider ContainerRegistryUrl => containerRegistryUrl;
        private IManifestExpressionProvider ContainerRegistryManagedIdentityId => containerRegistryManagedIdentityId;
        private IManifestExpressionProvider ClientId => clientId;

        private readonly Dictionary<IResource, ContainerAppContext> _containerApps = [];

        public async Task<AzureBicepResource> CreateContainerAppAsync(IResource resource, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            var context = await ProcessResourceAsync(resource, executionContext, cancellationToken).ConfigureAwait(false);

            var construct = new AzureConstructResource(resource.Name, context.BuildContainerApp);

            construct.Annotations.Add(new ManifestPublishingCallbackAnnotation(construct.WriteToManifest));

            return construct;
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

        private sealed class ContainerAppContext(IResource resource, ContainerAppEnviromentContext containerAppEnviromentContext)
        {
            private readonly Dictionary<object, string> _allocatedParameters = [];
            private readonly Dictionary<string, BicepParameter> _bicepParameters = [];
            private readonly ContainerAppEnviromentContext _containerAppEnviromentContext = containerAppEnviromentContext;

            record struct EndpointMapping(string Scheme, string Host, int Port, int? TargetPort, bool IsHttpIngress, bool External);

            private readonly Dictionary<string, EndpointMapping> _endpointMapping = [];

            private (int? Port, bool Http2, bool External)? _httpIngress;
            private readonly List<int> _additionalPorts = [];

            private BicepParameter? _managedIdentityIdParameter;
            private BicepParameter? _containerRegistryUrlParameter;
            private BicepParameter? _containerRegistryManagedIdentityIdParameter;

            public IResource Resource => resource;

            // Set the parameters to add to the bicep file
            public Dictionary<string, IManifestExpressionProvider> Parameters { get; } = [];

            public List<ContainerAppEnvironmentVariable> EnvironmentVariables { get; } = [];

            public List<ContainerAppWritableSecret> Secrets { get; } = [];

            public List<BicepValue<string>> Args { get; } = [];

            public List<(ContainerAppVolume, ContainerAppVolumeMount)> Volumes { get; } = [];

            public void BuildContainerApp(ResourceModuleConstruct c)
            {
                var containerAppIdParam = AllocateParameter(_containerAppEnviromentContext.ContainerAppEnvironmentId);

                BicepParameter? containerImageParam = null;

                if (!resource.TryGetContainerImageName(out var containerImageName))
                {
                    AllocateContainerRegistryParameters();

                    containerImageParam = AllocateContainerImageParameter();
                }

                var containerAppResource = new ContainerApp(resource.Name, "2024-03-01")
                {
                    Name = resource.Name.ToLowerInvariant()
                };

                c.Add(containerAppResource);

                // TODO: Add managed identities only when required
                AddManagedIdentites(containerAppResource);

                containerAppResource.EnvironmentId = containerAppIdParam;

                var configuration = new ContainerAppConfiguration()
                {
                    ActiveRevisionsMode = ContainerAppActiveRevisionsMode.Single,
                };
                containerAppResource.Configuration = configuration;

                AddIngress(configuration);

                AddContainerRegistryParameters(configuration);
                AddSecrets(configuration);

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

                containerAppContainer.Image = containerImageParam is null ? containerImageName : containerImageParam;
                containerAppContainer.Name = resource.Name;

                AddEnvironmentVariablesAndCommandLineArgs(containerAppContainer);

                foreach (var (_, mountedVolume) in Volumes)
                {
                    containerAppContainer.VolumeMounts.Add(mountedVolume);
                }

                foreach (var (key, value) in Parameters)
                {
                    c.Resource.Parameters[key] = value;
                }

                if (resource.TryGetAnnotationsOfType<ContainerAppCustomizationAnnotation>(out var annotations))
                {
                    foreach (var a in annotations)
                    {
                        a.Configure(c, containerAppResource);
                    }
                }
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
                    _containerAppEnviromentContext.Logger.LogWarning("More than 5 additional ports are not supported. See https://learn.microsoft.com/en-us/azure/container-apps/ingress-overview#tcp for more details.");
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
                    var context = new EnvironmentCallbackContext(executionContext, cancellationToken: cancellationToken);

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
                                var managedIdentityParameter = AllocateManagedIdentityIdParameter();
                                secret.Identity = managedIdentityParameter;
                                secret.KeyVaultUri = new BicepValue<Uri>(argValue.Expression!);
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

                // TODO: Add managed identity only if needed
                AllocateManagedIdentityIdParameter();
                var clientIdParameter = AllocateParameter(_containerAppEnviromentContext.ClientId);
                EnvironmentVariables.Add(new ContainerAppEnvironmentVariable { Name = "AZURE_CLIENT_ID", Value = clientIdParameter });
            }

            private static BicepValue<string> ResolveValue(object val)
            {
                return val switch
                {
                    BicepValue<string> s => s,
                    string s => s,
                    BicepValueFormattableString fs => Interpolate(fs),
                    BicepParameter p => p,
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
                            ContainerMountType.BindMount => ($"{bindMountIndex}", $"bm{bindMountIndex}"),
                            ContainerMountType.Volume => ($"{volumeIndex}", $"v{volumeIndex}"),
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

                        var volumeStorageParameter = AllocateVolumeStorageAccount(volume.Type, index);

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
                        var domain = AllocateParameter(_containerAppEnviromentContext.ContainerAppDomain);

                        return external ? BicepFunction.Interpolate($$"""{{prefix}}{{host}}.{{domain}}{{suffix}}""") : BicepFunction.Interpolate($$"""{{prefix}}{{host}}.internal.{{domain}}{{suffix}}""");
                    }

                    return $"{prefix}{host}{suffix}";
                }

                return property switch
                {
                    EndpointProperty.Url => GetHostValue($"{scheme}://", suffix: isHttpIngress ? null : $":{port}"),
                    EndpointProperty.Host or EndpointProperty.IPV4Host => GetHostValue(),
                    EndpointProperty.Port => port.ToString(CultureInfo.InvariantCulture),
                    EndpointProperty.TargetPort => targetPort is null ? AllocateTargetPortParameter() : targetPort,
                    EndpointProperty.Scheme => scheme,
                    _ => throw new NotSupportedException(),
                };
            }

            private async Task<(object, SecretType)> ProcessValueAsync(object value, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken, SecretType secretType = SecretType.None)
            {
                if (value is string s)
                {
                    return (s, secretType);
                }

                if (value is EndpointReference ep)
                {
                    var context = ep.Resource == resource
                        ? this
                        : await _containerAppEnviromentContext.ProcessResourceAsync(ep.Resource, executionContext, cancellationToken).ConfigureAwait(false);

                    var mapping = context._endpointMapping[ep.EndpointName];

                    var url = GetValue(mapping, EndpointProperty.Url);

                    return (url, secretType);
                }

                if (value is ConnectionStringReference cs)
                {
                    return await ProcessValueAsync(cs.Resource.ConnectionStringExpression, executionContext, cancellationToken, secretType: SecretType.Normal).ConfigureAwait(false);
                }

                if (value is IResourceWithConnectionString csrs)
                {
                    return await ProcessValueAsync(csrs.ConnectionStringExpression, executionContext, cancellationToken, secretType: SecretType.Normal).ConfigureAwait(false);
                }

                if (value is ParameterResource param)
                {
                    // This gets translated to a parameter 
                    var parameterName = AllocateParameter(param);

                    return (parameterName, param.Secret ? SecretType.Normal : secretType);
                }

                if (value is BicepOutputReference output)
                {
                    var parameterName = AllocateParameter(output);

                    return (parameterName, secretType);
                }

                if (value is BicepSecretOutputReference secretOutputReference)
                {
                    // Externalize secret outputs so azd can fill them in
                    var parameterName = AllocateParameter(secretOutputReference);

                    return (parameterName, SecretType.KeyVault);
                }

                if (value is EndpointReferenceExpression epExpr)
                {
                    var context = epExpr.Endpoint.Resource == resource
                        ? this
                        : await _containerAppEnviromentContext.ProcessResourceAsync(epExpr.Endpoint.Resource, executionContext, cancellationToken).ConfigureAwait(false);

                    var mapping = context._endpointMapping[epExpr.Endpoint.EndpointName];

                    var val = GetValue(mapping, epExpr.Property);

                    return (val, secretType);
                }

                if (value is ReferenceExpression expr)
                {
                    var args = new object[expr.ValueProviders.Count];
                    var index = 0;
                    var finalSecretType = SecretType.None;

                    foreach (var vp in expr.ValueProviders)
                    {
                        var (val, secret) = await ProcessValueAsync(vp, executionContext, cancellationToken, secretType).ConfigureAwait(false);

                        // Special case references to keyvault secrets
                        if (expr.Format == "{0}" && expr.ValueProviders.Count == 1 && secret == SecretType.KeyVault)
                        {
                            return (val, secret);
                        }

                        if (secret != SecretType.None)
                        {
                            finalSecretType = SecretType.Normal;
                        }

                        args[index++] = val;
                    }

                    return (new BicepValueFormattableString(expr.Format, args), finalSecretType);

                }

                throw new NotSupportedException("Unsupported value type " + value.GetType());
            }

            private BicepParameter AllocateVolumeStorageAccount(ContainerMountType type, string volumeIndex)
            {
                return AllocateParameter(VolumeStorageExpression.GetVolumeStorage(resource, type, volumeIndex));
            }

            private BicepParameter AllocateContainerImageParameter()
                => AllocateParameter(ProjectResourceExpression.GetContainerImageExpression((ProjectResource)resource));

            private BicepValue<int> AllocateTargetPortParameter()
                => AllocateParameter(ProjectResourceExpression.GetContainerPortExpression((ProjectResource)resource));

            private BicepParameter AllocateManagedIdentityIdParameter()
                => _managedIdentityIdParameter ??= AllocateParameter(_containerAppEnviromentContext.ManagedIdentityId);

            private void AllocateContainerRegistryParameters()
            {
                _containerRegistryUrlParameter ??= AllocateParameter(_containerAppEnviromentContext.ContainerRegistryUrl);
                _containerRegistryManagedIdentityIdParameter ??= AllocateParameter(_containerAppEnviromentContext.ContainerRegistryManagedIdentityId);
            }

            private BicepParameter AllocateParameter(IManifestExpressionProvider parameter, Type? type = null)
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

                if (!_bicepParameters.TryGetValue(parameterName, out var bicepParameter))
                {
                    var isSecure = parameter is BicepSecretOutputReference || parameter is ParameterResource { Secret: true };

                    _bicepParameters[parameterName] = bicepParameter = new BicepParameter(parameterName, type ?? typeof(string)) { IsSecure = isSecure };
                }

                Parameters[parameterName] = parameter;
                return bicepParameter;
            }

            private void AddIngress(ContainerAppConfiguration config)
            {
                if (_httpIngress is null && _additionalPorts.Count == 0)
                {
                    return;
                }

                // Now we map the remainig endpoints. These should be internal only tcp/http based endpoints
                var skipAdditionalPort = 0;

                var caIngress = new ContainerAppIngressConfiguration();

                if (_httpIngress is { } ingress)
                {
                    caIngress.External = ingress.External;
                    caIngress.TargetPort = ingress.Port ?? AllocateTargetPortParameter();
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

            private void AddSecrets(ContainerAppConfiguration config)
            {
                if (Secrets.Count == 0)
                {
                    return;
                }

                config.Secrets = [];

                foreach (var s in Secrets)
                {
                    config.Secrets.Add(s);
                }
            }

            private void AddManagedIdentites(ContainerApp app)
            {
                if (_managedIdentityIdParameter is null)
                {
                    return;
                }

                // REVIEW: This is is a little hacky, we should probably have a better way to do this
                var id = BicepFunction.Interpolate($"{_managedIdentityIdParameter}").Compile().ToString();

                app.Identity = new ManagedServiceIdentity()
                {
                    ManagedServiceIdentityType = ManagedServiceIdentityType.UserAssigned,
                    UserAssignedIdentities = new()
                    {
                        [id] = new UserAssignedIdentityDetails()
                    }
                };
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

    // REVIEW: BicepFunction.Interpolate is buggy and doesn't handle nested formattable strings correctly
    // This is a workaround to handle nested formattable strings until the bug is fixed.
    private static BicepValue<string> Interpolate(BicepValueFormattableString text)
    {
        var formatStringBuilder = new StringBuilder();
        var arguments = new List<BicepValue<string>>();

        void ProcessFormattableString(BicepValueFormattableString formattableString, int argumentIndex)
        {
            var span = formattableString.Format.AsSpan();
            var skip = 0;

            foreach (var match in Regex.EnumerateMatches(span, @"{\d+}"))
            {
                formatStringBuilder.Append(span[..(match.Index - skip)]);

                var argument = formattableString.GetArgument(argumentIndex);

                if (argument is BicepValueFormattableString nested)
                {
                    // Inline the nested formattable string
                    ProcessFormattableString(nested, 0);
                }
                else
                {
                    formatStringBuilder.Append(CultureInfo.InvariantCulture, $"{{{arguments.Count}}}");
                    if (argument is BicepValue<string> bicepValue)
                    {
                        arguments.Add(bicepValue);
                    }
                    else if (argument is string s)
                    {
                        arguments.Add(s);
                    }
                    else if (argument is BicepParameter bicepParameter)
                    {
                        arguments.Add(bicepParameter);
                    }
                    else
                    {
                        throw new NotSupportedException($"{argument} is not supported");
                    }
                }

                argumentIndex++;
                span = span[(match.Index + match.Length - skip)..];
                skip = match.Index + match.Length;
            }

            formatStringBuilder.Append(span);
        }

        ProcessFormattableString(text, 0);

        var formatString = formatStringBuilder.ToString();

        if (formatString == "{0}")
        {
            return arguments[0];
        }

        return BicepFunction.Interpolate(new BicepValueFormattableString(formatString, [.. arguments]));
    }

    /// <summary>
    /// A custom FormattableString implementation that allows us to inline nested formattable strings.
    /// </summary>
    private sealed class BicepValueFormattableString(string formatString, object[] values) : FormattableString
    {
        public override int ArgumentCount => values.Length;
        public override string Format => formatString;
        public override object? GetArgument(int index) => values[index];
        public override object?[] GetArguments() => values;
        public override string ToString(IFormatProvider? formatProvider) => Format;
        public override string ToString() => formatString;
    }

    /// <summary>
    /// These are referencing outputs from azd's main.bicep file. We represent the global namespace in the manifest
    /// by using {.outputs.property} expressions.
    /// </summary>
    private sealed class AzureContainerAppsEnvironment(string outputName) : IManifestExpressionProvider
    {
        public string ValueExpression => $"{{.outputs.{outputName}}}";

        public static IManifestExpressionProvider MANAGED_IDENTITY_CLIENT_ID => GetExpression("MANAGED_IDENTITY_CLIENT_ID");
        public static IManifestExpressionProvider MANAGED_IDENTITY_NAME => GetExpression("MANAGED_IDENTITY_NAME");
        public static IManifestExpressionProvider MANAGED_IDENTITY_PRINCIPAL_ID => GetExpression("MANAGED_IDENTITY_PRINCIPAL_ID");
        public static IManifestExpressionProvider AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID => GetExpression("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID");
        public static IManifestExpressionProvider AZURE_CONTAINER_REGISTRY_ENDPOINT => GetExpression("AZURE_CONTAINER_REGISTRY_ENDPOINT");
        public static IManifestExpressionProvider AZURE_CONTAINER_APPS_ENVIRONMENT_ID => GetExpression("AZURE_CONTAINER_APPS_ENVIRONMENT_ID");
        public static IManifestExpressionProvider AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN => GetExpression("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN");

        private static IManifestExpressionProvider GetExpression(string propertyExpression) =>
            new AzureContainerAppsEnvironment(propertyExpression);
    }

    private sealed class ProjectResourceExpression(ProjectResource projectResource, string propertyExpression) : IManifestExpressionProvider
    {
        public string ValueExpression => $"{{{projectResource.Name}.{propertyExpression}}}";

        public static IManifestExpressionProvider GetContainerImageExpression(ProjectResource p) =>
            new ProjectResourceExpression(p, "containerImage");

        public static IManifestExpressionProvider GetContainerPortExpression(ProjectResource p) =>
            new ProjectResourceExpression(p, "containerPort");
    }

    /// <summary>
    /// Generates expressions for the volume storage account. That azd creates.
    /// </summary>
    private sealed class VolumeStorageExpression(IResource resource, ContainerMountType type, string index) : IManifestExpressionProvider
    {
        public string ValueExpression => type switch
        {
            ContainerMountType.BindMount => $"{{{resource.Name}.bindMounts.{index}.storage}}",
            ContainerMountType.Volume => $"{{{resource.Name}.volumes.{index}.storage}}",
            _ => throw new NotSupportedException()
        };

        public static IManifestExpressionProvider GetVolumeStorage(IResource resource, ContainerMountType type, string index) =>
            new VolumeStorageExpression(resource, type, index);
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

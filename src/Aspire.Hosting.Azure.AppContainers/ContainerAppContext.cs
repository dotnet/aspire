// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.Resources;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure;

internal sealed class ContainerAppContext(IResource resource, ContainerAppEnvironmentContext containerAppEnvironmentContext)
{
    private readonly ContainerAppEnvironmentContext _containerAppEnvironmentContext = containerAppEnvironmentContext;

    public IResource Resource => resource;

    // Endpoint state after processing
    record struct EndpointMapping(string Scheme, string Host, int Port, int? TargetPort, bool IsHttpIngress, bool External);
    private readonly Dictionary<string, EndpointMapping> _endpointMapping = [];
    private (int? Port, bool Http2, bool External)? _httpIngress;
    private readonly List<int> _additionalPorts = [];

    // Resolved environment variables and command line args
    // These contain the values that need to be further transformed into
    // bicep compatible values
    public Dictionary<string, object> EnvironmentVariables { get; } = [];
    public List<object> Args { get; } = [];
    public Dictionary<string, (ContainerMountAnnotation, IManifestExpressionProvider)> Volumes { get; } = [];

    // Bicep build state
    private ProvisioningParameter? _containerRegistryUrlParameter;
    private ProvisioningParameter? _containerRegistryManagedIdentityIdParameter;
    public Dictionary<string, KeyVaultService> KeyVaultRefs { get; } = [];
    public Dictionary<string, KeyVaultSecret> KeyVaultSecretRefs { get; } = [];
    private AzureResourceInfrastructure? _infrastructure;
    public AzureResourceInfrastructure Infra => _infrastructure ?? throw new InvalidOperationException("Infra is not set");

    public void BuildContainerApp(AzureResourceInfrastructure infra)
    {
        _infrastructure = infra;
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

            containerAppIdentityId = appIdentityResource.Id.AsProvisioningParameter(infra);

            var id = BicepFunction.Interpolate($"{containerAppIdentityId}").Compile().ToString();

            containerAppResource.Identity.ManagedServiceIdentityType = ManagedServiceIdentityType.UserAssigned;
            containerAppResource.Identity.UserAssignedIdentities[id] = new UserAssignedIdentityDetails();
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

        var template = new ContainerAppTemplate();
        containerAppResource.Template = template;

        template.Scale = new ContainerAppScale()
        {
            MinReplicas = resource.GetReplicaCount()
        };

        var containerAppContainer = new ContainerAppContainer();
        template.Containers = [containerAppContainer];

        containerAppContainer.Image = containerImageParam is null ? containerImageName! : containerImageParam;
        containerAppContainer.Name = resource.Name;

        SetEntryPoint(containerAppContainer);
        AddEnvironmentVariablesAndCommandLineArgs(
            containerAppContainer,
            configuration,
            containerAppIdentityId);
        AddAzureClientId(appIdentityAnnotation?.IdentityResource, containerAppContainer);
        AddVolumes(template, containerAppContainer);

        // Keyvault
        foreach (var (_, v) in KeyVaultRefs)
        {
            infra.Add(v);
        }

        foreach (var (_, v) in KeyVaultSecretRefs)
        {
            infra.Add(v);
        }

        infra.Add(containerAppResource);

        if (resource.TryGetAnnotationsOfType<AzureContainerAppCustomizationAnnotation>(out var annotations))
        {
            foreach (var a in annotations)
            {
                a.Configure(infra, containerAppResource);
            }
        }
    }

    private void AddVolumes(ContainerAppTemplate template, ContainerAppContainer containerAppContainer)
    {
        foreach (var (volumeName, (volume, storageName)) in Volumes)
        {
            var containerAppVolume = new ContainerAppVolume
            {
                Name = volumeName,
                StorageType = ContainerAppStorageType.AzureFile,
                StorageName = storageName.AsProvisioningParameter(Infra),
            };

            template.Volumes.Add(containerAppVolume);

            var containerAppVolumeMount = new ContainerAppVolumeMount
            {
                VolumeName = volumeName,
                MountPath = volume.Target,
            };

            containerAppContainer.VolumeMounts.Add(containerAppVolumeMount);
        }
    }

    private void AddAzureClientId(IAppIdentityResource? appIdentityResource, ContainerAppContainer containerAppContainer)
    {
        if (appIdentityResource is not null)
        {
            containerAppContainer.Env.Add(new ContainerAppEnvironmentVariable
            {
                Name = "AZURE_CLIENT_ID",
                Value = AllocateParameter(appIdentityResource.ClientId)
            });
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

    public async Task ProcessResourceAsync(CancellationToken cancellationToken)
    {
        ProcessEndpoints();
        ProcessVolumes();

        await ProcessEnvironmentAsync(cancellationToken).ConfigureAwait(false);
        await ProcessArgumentsAsync(cancellationToken).ConfigureAwait(false);
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

    private async Task ProcessArgumentsAsync(CancellationToken cancellationToken)
    {
        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var commandLineArgsCallbackAnnotations))
        {
            var context = new CommandLineArgsCallbackContext(Args, cancellationToken: cancellationToken)
            {
                ExecutionContext = _containerAppEnvironmentContext.ExecutionContext,
            };

            foreach (var c in commandLineArgsCallbackAnnotations)
            {
                await c.Callback(context).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessEnvironmentAsync(CancellationToken cancellationToken)
    {
        if (resource.TryGetAnnotationsOfType<EnvironmentCallbackAnnotation>(out var environmentCallbacks))
        {
            var context = new EnvironmentCallbackContext(_containerAppEnvironmentContext.ExecutionContext, resource, EnvironmentVariables, cancellationToken: cancellationToken);

            foreach (var c in environmentCallbacks)
            {
                await c.Callback(context).ConfigureAwait(false);
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

                var storageName = _containerAppEnvironmentContext.Environment.GetVolumeStorage(resource, volume, volumeIndex);

                Volumes[volumeName] = (volume, storageName);

                if (volume.Type == ContainerMountType.BindMount)
                {
                    bindMountIndex++;
                }
                else
                {
                    volumeIndex++;
                }

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
            EndpointProperty.TargetPort => targetPort is null ? AllocateContainerPortParameter() : $"{targetPort}",
            EndpointProperty.Scheme => scheme,
            _ => throw new NotSupportedException(),
        };
    }

    private (object, SecretType) ProcessValue(object value, SecretType secretType = SecretType.None, object? parent = null)
    {
        if (value is string s)
        {
            return (s, secretType);
        }

        if (value is EndpointReference ep)
        {
            var context = ep.Resource == resource
                ? this
                : _containerAppEnvironmentContext.GetContainerAppContext(ep.Resource);

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
            return ProcessValue(cs.Resource.ConnectionStringExpression, secretType: secretType, parent: parent);
        }

        if (value is IResourceWithConnectionString csrs)
        {
            return ProcessValue(csrs.ConnectionStringExpression, secretType: secretType, parent: parent);
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
                : _containerAppEnvironmentContext.GetContainerAppContext(epExpr.Endpoint.Resource);

            var mapping = context._endpointMapping[epExpr.Endpoint.EndpointName];

            var val = GetValue(mapping, epExpr.Property);

            return (val, secretType);
        }

        if (value is ReferenceExpression expr)
        {
            // Special case simple expressions
            if (expr.Format == "{0}" && expr.ValueProviders.Count == 1)
            {
                return ProcessValue(expr.ValueProviders[0], secretType, parent: parent);
            }

            var args = new object[expr.ValueProviders.Count];
            var index = 0;
            var finalSecretType = SecretType.None;

            foreach (var vp in expr.ValueProviders)
            {
                var (val, secret) = ProcessValue(vp, secretType, parent: expr);

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
        var secret = secretOutputReference.AsKeyVaultSecret(Infra);

        return secret.Properties.SecretUri;
    }

    private ProvisioningParameter AllocateContainerImageParameter()
        => AllocateParameter(new ContainerImageReference(resource));

    private BicepValue<string> AllocateContainerPortParameter()
        => AllocateParameter(new ContainerPortReference(resource));

    private static BicepValue<int> AsInt(BicepValue<string> value)
    {
        return new FunctionCallExpression(new IdentifierExpression("int"), value.Compile());
    }

    private void AllocateContainerRegistryParameters()
    {
        _containerRegistryUrlParameter ??= AllocateParameter(_containerAppEnvironmentContext.Environment.ContainerRegistryUrl);
        _containerRegistryManagedIdentityIdParameter ??= AllocateParameter(_containerAppEnvironmentContext.Environment.ContainerRegistryManagedIdentityId);
    }

    private ProvisioningParameter AllocateParameter(IManifestExpressionProvider parameter, SecretType secretType = SecretType.None)
    {
        return parameter.AsProvisioningParameter(Infra, isSecure: secretType != SecretType.None);
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
            caIngress.TargetPort = ingress.Port ?? AsInt(AllocateContainerPortParameter());
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

    private void AddEnvironmentVariablesAndCommandLineArgs(
        ContainerAppContainer container,
        ContainerAppConfiguration containerAppConfiguration,
        BicepValue<string>? containerAppIdentityId)
    {
        if (EnvironmentVariables.Count > 0)
        {
            container.Env = [];

            foreach (var kv in EnvironmentVariables)
            {
                var (val, secretType) = ProcessValue(kv.Value);

                var argValue = ResolveValue(val);

                if (secretType != SecretType.None)
                {
                    var secretName = kv.Key.Replace("_", "-").ToLowerInvariant();

                    containerAppConfiguration.Secrets ??= [];

                    // Get or add the secret
                    var secret = containerAppConfiguration.Secrets
                                    .FirstOrDefault(s => s.Value?.Name.Value == secretName)
                                    ?.Value;

                    if (secret is null)
                    {
                        secret = new ContainerAppWritableSecret()
                        {
                            Name = secretName
                        };

                        if (secretType == SecretType.KeyVault)
                        {
                            // TODO: this should be able to use ToUri(), but it hit an issue
                            secret.KeyVaultUri = new BicepValue<Uri>(((BicepExpression?)argValue)!);

                            if (containerAppIdentityId is not null)
                            {
                                secret.Identity = containerAppIdentityId;
                            }
                        }
                        else
                        {
                            secret.Value = argValue;
                        }

                        containerAppConfiguration.Secrets.Add(secret);
                    }

                    // The value is the secret name
                    val = secretName;
                }

                container.Env.Add(secretType switch
                {
                    SecretType.None => new ContainerAppEnvironmentVariable { Name = kv.Key, Value = argValue },
                    SecretType.Normal or SecretType.KeyVault => new ContainerAppEnvironmentVariable { Name = kv.Key, SecretRef = (string)val },
                    _ => throw new NotSupportedException()
                });
            }
        }

        if (Args.Count > 0)
        {
            container.Args = [];

            foreach (var arg in Args)
            {
                var (val, _) = ProcessValue(arg);

                var argValue = ResolveValue(val);

                container.Args.Add(argValue);
            }
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

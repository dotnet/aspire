// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.Resources;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure;

internal sealed class ContainerAppContext(IResource resource, ContainerAppEnvironmentContext containerAppEnvironmentContext)
    : BaseContainerAppContext(resource, containerAppEnvironmentContext)
{
    // Endpoint state after processing
    private (int? Port, bool Http2, bool External)? _httpIngress;
    private readonly List<int> _additionalPorts = [];

    public override void BuildContainerApp(AzureResourceInfrastructure infra)
    {
        _infrastructure = infra;
        // Write a fake parameter for the container app environment
        // so azd knows the Dashboard URL - see https://github.com/dotnet/aspire/issues/8449.
        // This is temporary until a real fix can be made in azd.
        AllocateParameter(_containerAppEnvironmentContext.Environment.ContainerAppDomain);

        var containerAppIdParam = AllocateParameter(_containerAppEnvironmentContext.Environment.ContainerAppEnvironmentId);

        ProvisioningParameter? containerImageParam = null;

        if (!TryGetContainerImageName(Resource, out var containerImageName))
        {
            AllocateContainerRegistryParameters();

            containerImageParam = AllocateContainerImageParameter();
        }

        var containerAppResource = CreateContainerApp();

        BicepValue<string>? containerAppIdentityId = null;

        if (Resource.TryGetLastAnnotation<AppIdentityAnnotation>(out var appIdentityAnnotation))
        {
            var appIdentityResource = appIdentityAnnotation.IdentityResource;

            containerAppIdentityId = appIdentityResource.Id.AsProvisioningParameter(infra);

            var id = BicepFunction.Interpolate($"{containerAppIdentityId}").Compile().ToString();

            containerAppResource.Identity.ManagedServiceIdentityType = ManagedServiceIdentityType.UserAssigned;
            containerAppResource.Identity.UserAssignedIdentities[id] = new UserAssignedIdentityDetails();
        }

        AddContainerRegistryManagedIdentity(containerAppResource.Identity);

        containerAppResource.EnvironmentId = containerAppIdParam;

        var configuration = containerAppResource.Configuration;

        AddIngress(configuration);

        AddContainerRegistryParameters(reg => configuration.Registries = reg);

        var template = new ContainerAppTemplate();
        containerAppResource.Template = template;

        template.Scale = new ContainerAppScale()
        {
            MinReplicas = Resource.GetReplicaCount()
        };

        var containerAppContainer = new ContainerAppContainer();
        template.Containers = [containerAppContainer];

        containerAppContainer.Image = containerImageParam is null ? containerImageName! : containerImageParam;
        containerAppContainer.Name = NormalizedContainerAppName;

        SetEntryPoint(containerAppContainer);
        AddEnvironmentVariablesAndCommandLineArgs(
            containerAppContainer,
            () => configuration.Secrets ??= [],
            containerAppIdentityId);
        AddAzureClientId(appIdentityAnnotation?.IdentityResource, containerAppContainer.Env);
        AddVolumes(template.Volumes, containerAppContainer);
        AddProbes(containerAppContainer);

        infra.Add(containerAppResource);

        if (Resource.TryGetAnnotationsOfType<AzureContainerAppCustomizationAnnotation>(out var annotations))
        {
            foreach (var a in annotations)
            {
                a.Configure(infra, containerAppResource);
            }
        }
    }

    private ContainerApp CreateContainerApp()
    {
        var containerApp = new ContainerApp(Infrastructure.NormalizeBicepIdentifier(Resource.Name))
        {
            Name = NormalizedContainerAppName
        };

        var configuration = new ContainerAppConfiguration()
        {
            ActiveRevisionsMode = ContainerAppActiveRevisionsMode.Single,
        };
        containerApp.Configuration = configuration;

        const string latestPreview = "2025-02-02-preview"; // these properties are currently only available in preview

        // default autoConfigureDataProtection to true for .NET projects
        if (Resource is ProjectResource)
        {
            containerApp.ResourceVersion = latestPreview;

            var value = new BicepValue<bool>(true);
            ((IBicepValue)value).Self = new BicepValueReference(configuration, "AutoConfigureDataProtection", ["runtime", "dotnet", "autoConfigureDataProtection"]);
            configuration.ProvisionableProperties["AutoConfigureDataProtection"] = value;
        }

        // default kind to functionapp for Azure Functions
        if (Resource.HasAnnotationOfType<AzureFunctionsAnnotation>())
        {
            containerApp.ResourceVersion = latestPreview;

            var value = new BicepValue<string>("functionapp");
            ((IBicepValue)value).Self = new BicepValueReference(containerApp, "Kind", ["kind"]);
            containerApp.ProvisionableProperties["Kind"] = value;
        }

        return containerApp;
    }

    protected override void ProcessEndpoints()
    {
        if (!Resource.TryGetEndpoints(out var endpoints) || !endpoints.Any())
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

            int? targetPort = (Resource, endpoint.UriScheme, endpoint.TargetPort, endpoint.Port) switch
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
            if (Resource is ProjectResource && IsHttpScheme(endpoint.UriScheme))
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

            var targetPort = httpIngress.Port ?? (Resource is ProjectResource ? null : 80);

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

                _endpointMapping[e.Name] = new(e.UriScheme, NormalizedContainerAppName, port, targetPort, true, httpIngress.External);
            }
        }

        if (endpointsByTargetPort.Count > 5)
        {
            _containerAppEnvironmentContext.Logger.LogWarning("More than 5 additional ports are not supported. See https://learn.microsoft.com/azure/container-apps/ingress-overview#tcp for more details.");
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
                _endpointMapping[e.Name] = new(e.UriScheme, NormalizedContainerAppName, e.Port ?? g.Port.Value, g.Port.Value, false, g.External);
            }
        }
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
        // https://learn.microsoft.com/azure/container-apps/ingress-how-to?pivots=azure-cli#use-additional-tcp-ports
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
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker.Resources;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Represents a context for publishing Docker Compose configurations for a distributed application.
/// </summary>
/// <remarks>
/// This context facilitates the generation of Docker Compose files using the provided application model,
/// publisher options, and execution context. It handles the allocation of ports for services and ensures
/// that the Docker Compose configuration file is created in the specified output path.
/// </remarks>
internal sealed class DockerComposePublishingContext(
    DistributedApplicationExecutionContext executionContext,
    DockerComposePublisherOptions publisherOptions,
    ILogger logger,
    CancellationToken cancellationToken = default)
{
    public readonly PortAllocator PortAllocator = new();
    private readonly Dictionary<IResource, ComposeServiceContext> _composeServices = [];

    private ILogger Logger => logger;
    private DockerComposePublisherOptions Options => publisherOptions;

    internal async Task WriteModel(DistributedApplicationModel model)
    {
        if (executionContext.IsRunMode)
        {
            return;
        }

        logger.StartGeneratingDockerCompose();

        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(publisherOptions.OutputPath);

        if (model.Resources.Count == 0)
        {
            logger.WriteMessage("No resources found in the model.");
            return;
        }

        var outputFile = await WriteDockerComposeOutput(model).ConfigureAwait(false);

        logger.FinishGeneratingDockerCompose(outputFile);
    }

    private async Task<string> WriteDockerComposeOutput(DistributedApplicationModel model)
    {
        var composeFile = new ComposeFile(publisherOptions.ExistingNetworkName);

        foreach (var r in model.Resources)
        {
            if (r.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var lastAnnotation) &&
                lastAnnotation == ManifestPublishingCallbackAnnotation.Ignore)
            {
                continue;
            }

            if (!r.IsContainer() && r is not ProjectResource)
            {
                continue;
            }

            var composeServiceContext = await ProcessResourceAsync(r).ConfigureAwait(false);

            var composeService = composeServiceContext.BuildComposeService();

            composeFile.AddService(r.Name.ToLowerInvariant(), composeService);
        }

        var composeOutput = composeFile.ToYamlString();
        var outputFile = Path.Combine(publisherOptions.OutputPath!, "docker-compose.yaml");
        Directory.CreateDirectory(publisherOptions.OutputPath!);
        await File.WriteAllTextAsync(outputFile, composeOutput, cancellationToken).ConfigureAwait(false);
        return outputFile;
    }

    private async Task<ComposeServiceContext> ProcessResourceAsync(IResource resource)
    {
        if (!_composeServices.TryGetValue(resource, out var context))
        {
            _composeServices[resource] = context = new(resource, this);
            await context.ProcessResourceAsync(executionContext, cancellationToken).ConfigureAwait(false);
        }

        return context;
    }

    private sealed class ComposeServiceContext(IResource resource, DockerComposePublishingContext composePublishingContext)
    {
        private record struct EndpointMapping(string Scheme, string Host, int Port, int? TargetPort, bool IsHttpIngress, bool External);

        private readonly Dictionary<object, string> _allocatedParameters = [];
        private readonly Dictionary<string, EndpointMapping> _endpointMapping = [];

        public IResource Resource => resource;

        public List<ComposeEnvironmentVariable> EnvironmentVariables { get; } = [];

        public List<ComposeCommand> Args { get; } = [];

        public Dictionary<string, object> Parameters { get; } = [];

        public List<ComposeVolume> Volumes { get; } = [];

        public ComposeService BuildComposeService()
        {
            if (!TryGetContainerImageName(resource, out var containerImageName))
            {
                composePublishingContext.Logger.FailedToGetContainerImage(resource.Name);
            }

            var composeService = new ComposeService(resource.Name.ToLowerInvariant(), composePublishingContext.Options.ExistingNetworkName);

            SetEntryPoint(composeService);
            AddEnvironmentVariablesAndCommandLineArgs(composeService);
            AddPorts(composeService);
            SetContainerImage(containerImageName, composeService);

            return composeService;
        }

        private void AddPorts(ComposeService composeService)
        {
            if (_endpointMapping.Count == 0)
            {
                return;
            }

            foreach (var (_, mapping) in _endpointMapping)
            {
                var port = mapping.Port.ToString(CultureInfo.InvariantCulture);
                var targetPort = mapping.TargetPort?.ToString(CultureInfo.InvariantCulture) ?? port;

                composeService.AddPort($"{port}:{targetPort}");
            }
        }

        private static void SetContainerImage(string? containerImageName, ComposeService composeService)
        {
            if (containerImageName is not null)
            {
                composeService.WithImage(containerImageName);
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
            if (!resource.TryGetEndpoints(out var endpoints))
            {
                return;
            }

            var endpointsList = endpoints.ToList();

            // Only http, https, and tcp are supported
            var unsupportedEndpoints = endpointsList.Where(e => e.UriScheme is not ("tcp" or "http" or "https")).ToArray();

            if (unsupportedEndpoints.Length > 0)
            {
                throw new NotSupportedException(
                    $"The endpoint(s) {string.Join(", ", unsupportedEndpoints.Select(e => $"'{e.Name}'"))} specify an unsupported scheme. The supported schemes are 'http', 'https', and 'tcp'.");
            }

            var endpointIndexMap = new Dictionary<string, int>();

            // This is used to determine if an endpoint should be treated as the Default endpoint.
            // Endpoints can come from 3 different sources (in this order):
            // 1. Kestrel configuration
            // 2. Default endpoints added by the framework
            // 3. Explicitly added endpoints
            // But wherever they come from, we treat the first one as Default, for each scheme.
            var httpSchemesEncountered = new HashSet<string>();

            // Allocate ports for the endpoints
            foreach (var endpoint in endpointsList)
            {
                endpointIndexMap[endpoint.Name] = endpointIndexMap.Count;

                int? targetPort = (resource, endpoint.UriScheme, endpoint.TargetPort, endpoint.Port) switch
                {
                    // The port was specified so use it
                    (_, _, { } target, _) => target,

                    // Container resources get their default listening port from the exposed port.
                    (ContainerResource, _, null, { } port) => port,

                    // Check whether the project view this endpoint as Default (for its scheme).
                    // If so, we don't specify the target port, as it will get one from the deployment tool.
                    (ProjectResource _, { } uriScheme, null, _) when IsHttpScheme(uriScheme) &&
                                                                           !httpSchemesEncountered.Contains(uriScheme) => null,

                    // Allocate a dynamic port
                    _ => composePublishingContext.PortAllocator.AllocatePort(),
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
                    (_, { } p0, { } p1) when p0 == p1 => null,

                    // Port was specified, so use it
                    (_, { } port, _) => port,

                    // We have a target port, not need to specify an exposedPort
                    // it will default to the targetPort
                    (_, null, { } _) => null,

                    // Let the tool infer the default http and https ports
                    ("http", null, null) => null,
                    ("https", null, null) => null,

                    // Other schemes just allocate a port
                    _ => composePublishingContext.PortAllocator.AllocatePort(),
                };

                if (exposedPort is { } ep)
                {
                    composePublishingContext.PortAllocator.AddUsedPort(ep);
                    endpoint.Port = ep;
                }

                if (targetPort is { } tp)
                {
                    composePublishingContext.PortAllocator.AddUsedPort(tp);
                    endpoint.TargetPort = tp;
                }
            }

            // First we group the endpoints by container port (aka destinations), this gives us the logical bindings or destinations
            var endpointsByTargetPort = endpointsList.GroupBy(e => e.TargetPort)
                .Select(
                    g => new
                    {
                        Port = g.Key,
                        Endpoints = g.ToArray(),
                        External = g.Any(e => e.IsExternal),
                        IsHttpOnly = g.All(e => e.UriScheme is "http" or "https"),
                        AnyH2 = g.Any(e => e.Transport is "http2"),
                        UniqueSchemes = g.Select(e => e.UriScheme).Distinct().ToArray(),
                        Index = g.Min(e => endpointIndexMap[e.Name]),
                    })
                .ToList();

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

                foreach (var e in httpIngress.Endpoints)
                {
                    var port = e.Port.GetValueOrDefault(targetPort.GetValueOrDefault(80));

                    _endpointMapping[e.Name] = new(e.UriScheme, resource.Name, port, targetPort, true, httpIngress.External);
                }
            }

            foreach (var g in endpointsByTargetPort)
            {
                if (g.Port is null)
                {
                    throw new NotSupportedException("Container port is required for all endpoints");
                }

                foreach (var e in g.Endpoints)
                {
                    _endpointMapping[e.Name] = new(e.UriScheme, resource.Name, e.Port ?? g.Port.Value, g.Port.Value, false, g.External);
                }
            }

            return;

            static bool IsHttpScheme(string scheme) => scheme is "http" or "https";
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
                    var value = await ProcessValueAsync(arg, executionContext, cancellationToken).ConfigureAwait(false);

                    if (value is not string str)
                    {
                        throw new NotSupportedException("Command line args must be strings");
                    }

                    Args.Add(new(str));
                }
            }
        }

        private async Task ProcessEnvironmentAsync(DistributedApplicationExecutionContext executionContext,
            CancellationToken cancellationToken)
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
                    var value = await ProcessValueAsync(kv.Value, executionContext, cancellationToken).ConfigureAwait(false);

                    EnvironmentVariables.Add(new ComposeEnvironmentVariable(kv.Key, value.ToString()));
                }
            }
        }

        private static void ProcessVolumes()
        {
            // not implemented
        }

        private string GetValue(EndpointMapping mapping, EndpointProperty property)
        {
            var (scheme, host, port, targetPort, isHttpIngress, _) = mapping;

            return property switch
            {
                EndpointProperty.Url => GetHostValue($"{scheme}://", suffix: isHttpIngress ? null : $":{port}"),
                EndpointProperty.Host or EndpointProperty.IPV4Host => GetHostValue(),
                EndpointProperty.Port => port.ToString(CultureInfo.InvariantCulture),
                EndpointProperty.HostAndPort => GetHostValue(suffix: $":{port}"),
                EndpointProperty.TargetPort => $"{targetPort ?? composePublishingContext.PortAllocator.AllocatePort()}",
                EndpointProperty.Scheme => scheme,
                _ => throw new NotSupportedException(),
            };

            string GetHostValue(string? prefix = null, string? suffix = null)
            {
                return $"{prefix}{host}{suffix}";
            }
        }

        private async Task<object> ProcessValueAsync(
            object value,
            DistributedApplicationExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            if (value is string s)
            {
                return s;
            }

            if (value is EndpointReference ep)
            {
                var context = ep.Resource == resource
                    ? this
                    : await composePublishingContext.ProcessResourceAsync(ep.Resource)
                        .ConfigureAwait(false);

                var mapping = context._endpointMapping[ep.EndpointName];

                var url = GetValue(mapping, EndpointProperty.Url);

                return url;
            }

            if (value is ParameterResource param)
            {
                return AllocateParameter(param) ?? throw new InvalidOperationException("Parameter name is null");
            }

            if (value is ConnectionStringReference cs)
            {
                return await ProcessValueAsync(cs.Resource.ConnectionStringExpression, executionContext, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (value is IResourceWithConnectionString csrs)
            {
                return await ProcessValueAsync(
                        csrs.ConnectionStringExpression, executionContext, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (value is EndpointReferenceExpression epExpr)
            {
                var context = epExpr.Endpoint.Resource == resource
                    ? this
                    : await composePublishingContext
                        .ProcessResourceAsync(epExpr.Endpoint.Resource).ConfigureAwait(false);

                var mapping = context._endpointMapping[epExpr.Endpoint.EndpointName];

                var val = GetValue(mapping, epExpr.Property);

                return val;
            }

            if (value is ReferenceExpression expr)
            {
                // Special case simple expressions
                if (expr is {Format: "{0}", ValueProviders.Count: 1})
                {
                    return await ProcessValueAsync(expr.ValueProviders[0], executionContext, cancellationToken)
                        .ConfigureAwait(false);
                }

                var args = new object[expr.ValueProviders.Count];
                var index = 0;

                foreach (var vp in expr.ValueProviders)
                {
                    var val = await ProcessValueAsync(vp, executionContext, cancellationToken) // pass parent, and handle.
                        .ConfigureAwait(false);

                    args[index++] = val;
                }

                return args;

            }

            return value;
        }

        private string? AllocateParameter(IManifestExpressionProvider parameter)
        {
            if (!_allocatedParameters.TryGetValue(parameter, out var parameterName))
            {
                parameterName = parameter.ValueExpression.Replace("{", "").Replace("}", "").Replace(".", "_").Replace("-", "_")
                    .ToLowerInvariant();

                if (parameterName[0] == '_')
                {
                    parameterName = parameterName[1..];
                }

                _allocatedParameters[parameter] = parameterName;
            }

            if (!_allocatedParameters.TryGetValue(parameterName, out var provisioningParameter))
            {
                // _allocatedParameters[parameterName] = provisioningParameter;
            }

            Parameters[parameterName] = parameter;
            return provisioningParameter;
        }

        private void SetEntryPoint(ComposeService composeService)
        {
            if (resource is ContainerResource {Entrypoint: { } entrypoint})
            {
                composeService.WithEntrypoint(entrypoint);
            }
        }

        private void AddEnvironmentVariablesAndCommandLineArgs(ComposeService composeService)
        {
            if (EnvironmentVariables.Count > 0)
            {
                foreach (var ev in EnvironmentVariables)
                {
                    composeService.AddEnvironmentVariable(ev);
                }
            }

            if (Args.Count > 0)
            {
                foreach (var command in Args)
                {
                    composeService.AddCommand(command);
                }
            }
        }
    }
}

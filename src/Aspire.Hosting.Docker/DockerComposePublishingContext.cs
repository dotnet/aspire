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

        foreach (var resource in model.Resources)
        {
            if (resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var lastAnnotation) &&
                lastAnnotation == ManifestPublishingCallbackAnnotation.Ignore)
            {
                continue;
            }

            if (!resource.IsContainer() && resource is not ProjectResource)
            {
                continue;
            }

            var composeServiceContext = await ProcessResourceAsync(resource).ConfigureAwait(false);

            var composeService = composeServiceContext.BuildComposeService();

            composeFile.AddService(resource.Name.ToLowerInvariant(), composeService);
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
        private readonly Dictionary<string, string> _allocatableParameters = [];
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
                var port = endpoint.Port ?? composePublishingContext.PortAllocator.AllocatePort();
                composePublishingContext.PortAllocator.AddUsedPort(port);
                var targetPort = endpoint.TargetPort ?? port;
                _endpointMapping[endpoint.Name] = new(endpoint.UriScheme, resource.Name, port, targetPort, false, endpoint.IsExternal);
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

                    Args.Add(new(str));
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
                    var value = await ProcessValueAsync(kv.Value).ConfigureAwait(false);

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
                        : await composePublishingContext.ProcessResourceAsync(epExpr.Endpoint.Resource).ConfigureAwait(false);

                    var mapping = context._endpointMapping[epExpr.Endpoint.EndpointName];

                    var val = GetValue(mapping, epExpr.Property);

                    return val;
                }

                if (value is ReferenceExpression expr)
                {
                    // Special case simple expressions
                    if (expr is {Format: "{0}", ValueProviders.Count: 1})
                    {
                        value = expr.ValueProviders[0];
                        continue;
                    }

                    var args = new object[expr.ValueProviders.Count];
                    var index = 0;

                    foreach (var vp in expr.ValueProviders)
                    {
                        var val = await ProcessValueAsync(vp).ConfigureAwait(false);

                        args[index++] = val ?? throw new InvalidOperationException("Value is null");
                    }

                    return args;
                }

                // todo: ideally we should have processed all resources that we can before getting here...
                // This is probably going to include removing the resource from the model if its not processable during publishing in Docker - BicepResources?
                // The problem there is that we'd need to take reference on Azure hosting for that.
                // Approach: Maybe we filter the incoming resources and remove the ones that are not processable?
                if (value is IManifestExpressionProvider r)
                {
                    composePublishingContext.Logger.NotSupportedResourceWarning(nameof(value), r.GetType().Name);
                }

                return value; // todo: we need to never get here really...
            }
        }

        private string AllocateParameter(IManifestExpressionProvider parameter)
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

            if (!_allocatableParameters.TryGetValue(parameterName, out var allocatableParameter))
            {
                _allocatableParameters[parameterName] = string.Empty;
            }

            Parameters[parameterName] = parameter;
            return allocatableParameter ?? _allocatableParameters[parameterName];
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

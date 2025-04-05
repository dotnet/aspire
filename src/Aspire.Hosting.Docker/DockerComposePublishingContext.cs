// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Globalization;
using System.Text;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker.Resources;
using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes;
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
    IResourceContainerImageBuilder imageBuilder,
    ILogger logger,
    CancellationToken cancellationToken = default)
{
    public readonly IResourceContainerImageBuilder ImageBuilder = imageBuilder;
    public readonly DockerComposePublisherOptions PublisherOptions = publisherOptions;
    public readonly PortAllocator PortAllocator = new();
    private readonly Dictionary<string, (string Description, string? DefaultValue)> _env = [];
    private readonly Dictionary<IResource, ComposeServiceContext> _composeServices = [];

    private ILogger Logger => logger;

    internal async Task WriteModelAsync(DistributedApplicationModel model)
    {
        if (!executionContext.IsPublishMode)
        {
            logger.NotInPublishingMode();
            return;
        }

        logger.StartGeneratingDockerCompose();

        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(PublisherOptions.OutputPath);

        if (model.Resources.Count == 0)
        {
            logger.EmptyModel();
            return;
        }

        await WriteDockerComposeOutputAsync(model).ConfigureAwait(false);

        logger.FinishGeneratingDockerCompose(PublisherOptions.OutputPath);
    }

    public void AddEnv(string name, string description, string? defaultValue = null)
    {
        _env[name] = (description, defaultValue);
    }

    private async Task WriteDockerComposeOutputAsync(DistributedApplicationModel model)
    {
        var defaultNetwork = new Network
        {
            Name = PublisherOptions.ExistingNetworkName ?? "aspire",
            Driver = "bridge",
        };

        var composeFile = new ComposeFile();
        composeFile.AddNetwork(defaultNetwork);

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

            var composeService = await composeServiceContext.BuildComposeServiceAsync(cancellationToken).ConfigureAwait(false);

            HandleComposeFileVolumes(composeServiceContext, composeFile);

            composeService.Networks =
            [
                defaultNetwork.Name,
            ];

            composeFile.AddService(composeService);
        }

        var composeOutput = composeFile.ToYaml();
        var outputFile = Path.Combine(PublisherOptions.OutputPath!, "docker-compose.yaml");
        Directory.CreateDirectory(PublisherOptions.OutputPath!);
        await File.WriteAllTextAsync(outputFile, composeOutput, cancellationToken).ConfigureAwait(false);

        if (_env.Count == 0)
        {
            // No environment variables to write, so we can skip creating the .env file
            return;
        }

        // Write a .env file with the environment variable names
        // that are used in the compose file
        var envFile = Path.Combine(PublisherOptions.OutputPath!, ".env");
        using var envWriter = new StreamWriter(envFile);

        foreach (var entry in _env)
        {
            var (key, (description, defaultValue)) = entry;

            await envWriter.WriteLineAsync($"# {description}").ConfigureAwait(false);

            if (defaultValue is not null)
            {
                await envWriter.WriteLineAsync($"{key}={defaultValue}").ConfigureAwait(false);
            }
            else
            {
                await envWriter.WriteLineAsync($"{key}=").ConfigureAwait(false);
            }

            await envWriter.WriteLineAsync().ConfigureAwait(false);
        }

        await envWriter.FlushAsync().ConfigureAwait(false);
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

    private static void HandleComposeFileVolumes(ComposeServiceContext composeServiceContext, ComposeFile composeFile)
    {
        foreach (var volume in composeServiceContext.Volumes.Where(volume => volume.Type != "bind"))
        {
            if (composeFile.Volumes.ContainsKey(volume.Name))
            {
                continue;
            }

            var newVolume = new Volume
            {
                Name = volume.Name,
                Driver = volume.Driver ?? "local",
                External = volume.External,
            };

            composeFile.AddVolume(newVolume);
        }
    }

    private sealed class ComposeServiceContext(IResource resource, DockerComposePublishingContext composePublishingContext)
    {
        /// <summary>
        /// Most common shell executables used as container entrypoints in Linux containers.
        /// These are used to identify when a container's entrypoint is a shell that will execute commands.
        /// </summary>
        private static readonly HashSet<string> s_shellExecutables = new(StringComparer.OrdinalIgnoreCase)
        {
            "/bin/sh",
            "/bin/bash",
            "/sh",
            "/bash",
            "sh",
            "bash",
            "/usr/bin/sh",
            "/usr/bin/bash"
        };

        private record struct EndpointMapping(string Scheme, string Host, int InternalPort, int ExposedPort, bool IsHttpIngress);

        private readonly Dictionary<string, EndpointMapping> _endpointMapping = [];
        public Dictionary<string, string> EnvironmentVariables { get; } = [];
        private List<string> Commands { get; } = [];
        public List<Volume> Volumes { get; } = [];
        public bool IsShellExec { get; private set; }

        public async Task<Service> BuildComposeServiceAsync(CancellationToken cancellationToken)
        {
            if (composePublishingContext.PublisherOptions.BuildImages)
            {
                await composePublishingContext.ImageBuilder.BuildImageAsync(resource, cancellationToken).ConfigureAwait(false);
            }

            if (!TryGetContainerImageName(resource, out var containerImageName))
            {
                composePublishingContext.Logger.FailedToGetContainerImage(resource.Name);
            }

            var composeService = new Service
            {
                Name = resource.Name.ToLowerInvariant(),
            };

            SetEntryPoint(composeService);
            AddEnvironmentVariablesAndCommandLineArgs(composeService);
            AddPorts(composeService);
            AddVolumes(composeService);
            SetContainerImage(containerImageName, composeService);
            SetDependsOn(composeService);

            return composeService;
        }

        private void SetDependsOn(Service composeService)
        {
            if (resource.TryGetAnnotationsOfType<WaitAnnotation>(out var waitAnnotations))
            {
                foreach (var waitAnnotation in waitAnnotations)
                {
                    // We can only wait on other compose services
                    if (waitAnnotation.Resource is ProjectResource || waitAnnotation.Resource.IsContainer())
                    {
                        // https://docs.docker.com/compose/how-tos/startup-order/#control-startup
                        composeService.DependsOn[waitAnnotation.Resource.Name.ToLowerInvariant()] = new()
                        {
                            Condition = waitAnnotation.WaitType switch
                            {
                                // REVIEW: This only works if the target service has health checks,
                                // revisit this when we have a way to add health checks to the compose service
                                // WaitType.WaitUntilHealthy => "service_healthy",
                                WaitType.WaitForCompletion => "service_completed_successfully",
                                _ => "service_started",
                            },
                        };
                    }
                }
            }
        }

        private bool TryGetContainerImageName(IResource resourceInstance, out string? containerImageName)
        {
            // If the resource has a Dockerfile build annotation, we don't have the image name
            // it will come as a parameter
            if (resourceInstance.TryGetLastAnnotation<DockerfileBuildAnnotation>(out _) || resourceInstance is ProjectResource)
            {
                var imageEnvName = $"{resourceInstance.Name.ToUpperInvariant().Replace("-", "_")}_IMAGE";

                composePublishingContext.AddEnv(imageEnvName,
                                                $"Container image name for {resourceInstance.Name}",
                                                $"{resourceInstance.Name}:latest");

                containerImageName = $"${{{imageEnvName}}}";
                return true;
            }

            return resourceInstance.TryGetContainerImageName(out containerImageName);
        }

        private void AddVolumes(Service composeService)
        {
            if (Volumes.Count == 0)
            {
                return;
            }

            foreach (var volume in Volumes)
            {
                composeService.AddVolume(volume);
            }
        }

        private void AddPorts(Service composeService)
        {
            if (_endpointMapping.Count == 0)
            {
                return;
            }

            foreach (var (_, mapping) in _endpointMapping)
            {
                var internalPort = mapping.InternalPort.ToString(CultureInfo.InvariantCulture);
                var exposedPort = mapping.ExposedPort.ToString(CultureInfo.InvariantCulture);

                composeService.Ports.Add($"{exposedPort}:{internalPort}");
            }
        }

        private static void SetContainerImage(string? containerImageName, Service composeService)
        {
            if (containerImageName is not null)
            {
                composeService.Image = containerImageName;
            }
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
                var internalPort = endpoint.TargetPort ?? composePublishingContext.PortAllocator.AllocatePort();
                composePublishingContext.PortAllocator.AddUsedPort(internalPort);

                var exposedPort = composePublishingContext.PortAllocator.AllocatePort();
                composePublishingContext.PortAllocator.AddUsedPort(exposedPort);

                _endpointMapping[endpoint.Name] = new(endpoint.UriScheme, resource.Name, internalPort, exposedPort, false);
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

                    Commands.Add(new(str));
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

                RemoveHttpsServiceDiscoveryVariables(context.EnvironmentVariables);

                foreach (var kv in context.EnvironmentVariables)
                {
                    var value = await ProcessValueAsync(kv.Value).ConfigureAwait(false);

                    EnvironmentVariables[kv.Key] = value.ToString() ?? string.Empty;
                }
            }
        }

        private static void RemoveHttpsServiceDiscoveryVariables(Dictionary<string, object> environmentVariables)
        {
            // HACK: At the moment Docker Compose doesn't do anything with setting up certificates so
            //       we need to remove the https service discovery variables.

            var keysToRemove = environmentVariables
                .Where(kvp => kvp.Value is EndpointReference epRef && epRef.Scheme == "https" && kvp.Key.StartsWith("services__"))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                environmentVariables.Remove(key);
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

                var composeVolume = new Volume
                {
                    Name = volume.Source,
                    Type = volume.Type == ContainerMountType.BindMount ? "bind" : "volume",
                    Target = volume.Target,
                    Source = volume.Source,
                    ReadOnly = volume.IsReadOnly,
                };

                Volumes.Add(composeVolume);
            }
        }

        private static string GetValue(EndpointMapping mapping, EndpointProperty property)
        {
            var (scheme, host, internalPort, _, isHttpIngress) = mapping;

            return property switch
            {
                EndpointProperty.Url => GetHostValue($"{scheme}://", suffix: isHttpIngress ? null : $":{internalPort}"),
                EndpointProperty.Host or EndpointProperty.IPV4Host => GetHostValue(),
                EndpointProperty.Port => internalPort.ToString(CultureInfo.InvariantCulture),
                EndpointProperty.HostAndPort => GetHostValue(suffix: $":{internalPort}"),
                EndpointProperty.TargetPort => $"{internalPort}",
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
                    return AllocateParameter(param);
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
                    if (expr is { Format: "{0}", ValueProviders.Count: 1 })
                    {
                        return (await ProcessValueAsync(expr.ValueProviders[0]).ConfigureAwait(false)).ToString() ?? string.Empty;
                    }

                    var args = new object[expr.ValueProviders.Count];
                    var index = 0;

                    foreach (var vp in expr.ValueProviders)
                    {
                        var val = await ProcessValueAsync(vp).ConfigureAwait(false);
                        args[index++] = val ?? throw new InvalidOperationException("Value is null");
                    }

                    return string.Format(CultureInfo.InvariantCulture, expr.Format, args);
                }

                // If we don't know how to process the value, we just return it as an external reference
                if (value is IManifestExpressionProvider r)
                {
                    composePublishingContext.Logger.NotSupportedResourceWarning(nameof(value), r.GetType().Name);

                    return ResolveUnknownValue(r);
                }

                return value; // todo: we need to never get here really...
            }
        }

        private string ResolveUnknownValue(IManifestExpressionProvider parameter)
        {
            // Placeholder for resolving the actual parameter value
            // https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/#interpolation-syntax

            // Treat secrets as environment variable placeholders as for now
            // this doesn't handle generation of parameter values with defaults
            var env = parameter.ValueExpression.Replace("{", "")
                     .Replace("}", "")
                     .Replace(".", "_")
                     .Replace("-", "_")
                     .ToUpperInvariant();

            composePublishingContext.AddEnv(env, $"Unknown reference {parameter.ValueExpression}");

            return $"${{{env}}}";
        }

        private string ResolveParameterValue(ParameterResource parameter)
        {
            // Placeholder for resolving the actual parameter value
            // https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/#interpolation-syntax

            // Treat secrets as environment variable placeholders as for now
            // this doesn't handle generation of parameter values with defaults
            var env = parameter.Name.ToUpperInvariant().Replace("-", "_");

            composePublishingContext.AddEnv(env, $"Parameter {parameter.Name}",
                                            parameter.Secret || parameter.Default is null ? null : parameter.Value);

            return $"${{{env}}}";
        }

        private string AllocateParameter(ParameterResource parameter)
        {
            return ResolveParameterValue(parameter);
        }

        private void SetEntryPoint(Service composeService)
        {
            if (resource is ContainerResource { Entrypoint: { } entrypoint })
            {
                composeService.Entrypoint.Add(entrypoint);

                if (s_shellExecutables.Contains(entrypoint))
                {
                    IsShellExec = true;
                }
            }
        }

        private void AddEnvironmentVariablesAndCommandLineArgs(Service composeService)
        {
            if (EnvironmentVariables.Count > 0)
            {
                foreach (var variable in EnvironmentVariables)
                {
                    composeService.AddEnvironmentalVariable(variable.Key, variable.Value);
                }
            }

            if (Commands.Count > 0)
            {
                if (IsShellExec)
                {
                    var sb = new StringBuilder();
                    foreach (var command in Commands)
                    {
                        // Escape any environment variables expressions in the command
                        // to prevent them from being interpreted by the docker compose CLI
                        EnvVarEscaper.EscapeUnescapedEnvVars(command, sb);
                        composeService.Command.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else
                {
                    composeService.Command.AddRange(Commands);
                }
            }
        }
    }
}

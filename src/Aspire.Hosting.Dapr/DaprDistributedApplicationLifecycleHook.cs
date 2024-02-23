// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using static Aspire.Hosting.Dapr.CommandLineArgs;

namespace Aspire.Hosting.Dapr;

internal sealed class DaprDistributedApplicationLifecycleHook : IDistributedApplicationLifecycleHook, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DaprDistributedApplicationLifecycleHook> _logger;
    private readonly DaprOptions _options;

    private string? _onDemandResourcesRootPath;

    public DaprDistributedApplicationLifecycleHook(IConfiguration configuration, IHostEnvironment environment, ILogger<DaprDistributedApplicationLifecycleHook> logger, IOptions<DaprOptions> options)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
        _options = options.Value;
    }

    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        string appHostDirectory = _configuration["AppHost:Directory"] ?? throw new InvalidOperationException("Unable to obtain the application host directory.");

        var onDemandResourcesPaths = await StartOnDemandDaprComponentsAsync(appModel, cancellationToken).ConfigureAwait(false);

        var sideCars = new List<ExecutableResource>();

        var fileName = this._options.DaprPath
            ?? GetDefaultDaprPath()
            ?? throw new DistributedApplicationException("Unable to locate the Dapr CLI.");

        foreach (var resource in appModel.Resources)
        {
            if (!resource.TryGetLastAnnotation<DaprSidecarAnnotation>(out var daprAnnotation))
            {
                continue;
            }

            var daprSidecar = daprAnnotation.Sidecar;

            var sidecarOptionsAnnotation = daprSidecar.Annotations.OfType<DaprSidecarOptionsAnnotation>().LastOrDefault();

            var sidecarOptions = sidecarOptionsAnnotation?.Options;

            [return: NotNullIfNotNull(nameof(path))]
            string? NormalizePath(string? path)
            {
                if (path is null)
                {
                    return null;
                }

                return Path.GetFullPath(Path.Combine(appHostDirectory, path));
            }

            var aggregateResourcesPaths = sidecarOptions?.ResourcesPaths.Select(path => NormalizePath(path)).ToHashSet() ?? new HashSet<string>();

            var componentReferenceAnnotations = resource.Annotations.OfType<DaprComponentReferenceAnnotation>();

            foreach (var componentReferenceAnnotation in componentReferenceAnnotations)
            {
                if (componentReferenceAnnotation.Component.Options?.LocalPath is not null)
                {
                    var localPathDirectory = Path.GetDirectoryName(NormalizePath(componentReferenceAnnotation.Component.Options.LocalPath));

                    if (localPathDirectory is not null)
                    {
                        aggregateResourcesPaths.Add(localPathDirectory);
                    }
                }
                else if (onDemandResourcesPaths.TryGetValue(componentReferenceAnnotation.Component.Name, out var onDemandResourcesPath))
                {
                    string onDemandResourcesPathDirectory = Path.GetDirectoryName(onDemandResourcesPath)!;

                    if (onDemandResourcesPathDirectory is not null)
                    {
                        aggregateResourcesPaths.Add(onDemandResourcesPathDirectory);
                    }
                }
            }

            var daprAppPortArg = (int? port) => ModelNamedArg("--app-port", port);
            var daprGrpcPortArg = (string? port) => ModelNamedArg("--dapr-grpc-port", port);
            var daprHttpPortArg = (string? port) => ModelNamedArg("--dapr-http-port", port);
            var daprMetricsPortArg = (string? port) => ModelNamedArg("--metrics-port", port);
            var daprProfilePortArg = (string? port) => ModelNamedArg("--profile-port", port);
            var daprAppChannelAddressArg = (string? address) => ModelNamedArg("--app-channel-address", address);

            var appId = sidecarOptions?.AppId ?? resource.Name;

            var daprCommandLine =
                CommandLineBuilder
                    .Create(
                        fileName,
                        Command("run"),
                        daprAppPortArg(sidecarOptions?.AppPort),
                        ModelNamedArg("--app-channel-address", sidecarOptions?.AppChannelAddress),
                        ModelNamedArg("--app-health-check-path", sidecarOptions?.AppHealthCheckPath),
                        ModelNamedArg("--app-health-probe-interval", sidecarOptions?.AppHealthProbeInterval),
                        ModelNamedArg("--app-health-probe-timeout", sidecarOptions?.AppHealthProbeTimeout),
                        ModelNamedArg("--app-health-threshold", sidecarOptions?.AppHealthThreshold),
                        ModelNamedArg("--app-id", appId),
                        ModelNamedArg("--app-max-concurrency", sidecarOptions?.AppMaxConcurrency),
                        ModelNamedArg("--app-protocol", sidecarOptions?.AppProtocol),
                        ModelNamedArg("--config", NormalizePath(sidecarOptions?.Config)),
                        ModelNamedArg("--dapr-http-max-request-size", sidecarOptions?.DaprHttpMaxRequestSize),
                        ModelNamedArg("--dapr-http-read-buffer-size", sidecarOptions?.DaprHttpReadBufferSize),
                        ModelNamedArg("--dapr-internal-grpc-port", sidecarOptions?.DaprInternalGrpcPort),
                        ModelNamedArg("--dapr-listen-addresses", sidecarOptions?.DaprListenAddresses),
                        Flag("--enable-api-logging", sidecarOptions?.EnableApiLogging),
                        Flag("--enable-app-health-check", sidecarOptions?.EnableAppHealthCheck),
                        Flag("--enable-profiling", sidecarOptions?.EnableProfiling),
                        ModelNamedArg("--log-level", sidecarOptions?.LogLevel),
                        ModelNamedArg("--placement-host-address", sidecarOptions?.PlacementHostAddress),
                        ModelNamedArg("--resources-path", aggregateResourcesPaths),
                        ModelNamedArg("--run-file", NormalizePath(sidecarOptions?.RunFile)),
                        ModelNamedArg("--runtime-path", NormalizePath(sidecarOptions?.RuntimePath)),
                        ModelNamedArg("--unix-domain-socket", sidecarOptions?.UnixDomainSocket),
                        PostOptionsArgs(Args(sidecarOptions?.Command)));

            var daprCliResourceName = $"{daprSidecar.Name}-cli";
            var daprCli = new ExecutableResource(daprCliResourceName, fileName, appHostDirectory, daprCommandLine.Arguments.ToArray());

            resource.Annotations.Add(
                new EnvironmentCallbackAnnotation(
                    context =>
                    {
                        if (context.ExecutionContext.IsPublishMode)
                        {
                            return;
                        }

                        string? grpcEndpoint, httpEndpoint;

                        if (resource is ContainerResource)
                        {
                            // By default, the Dapr sidecar will listen on localhost, which is not accessible from the container.

                            grpcEndpoint = $"http://localhost:{{{{- portFor \"{daprCliResourceName}_grpc\" -}}}}";
                            httpEndpoint = $"http://localhost:{{{{- portFor \"{daprCliResourceName}_http\" -}}}}";

                            context.EnvironmentVariables.TryAdd("DAPR_GRPC_ENDPOINT", HostNameResolver.ReplaceLocalhostWithContainerHost(grpcEndpoint, _configuration));
                            context.EnvironmentVariables.TryAdd("DAPR_HTTP_ENDPOINT", HostNameResolver.ReplaceLocalhostWithContainerHost(httpEndpoint, _configuration));
                        }
                        else
                        {
                            grpcEndpoint = $"http://{{{{- addressFor \"{daprCliResourceName}_grpc\" -}}}}:{{{{- portFor \"{daprCliResourceName}_grpc\" -}}}}";
                            httpEndpoint = $"http://{{{{- addressFor \"{daprCliResourceName}_http\" -}}}}:{{{{- portFor \"{daprCliResourceName}_http\" -}}}}";
                            context.EnvironmentVariables.TryAdd("DAPR_GRPC_ENDPOINT", grpcEndpoint);
                            context.EnvironmentVariables.TryAdd("DAPR_HTTP_ENDPOINT", httpEndpoint);
                        }
                    }));

            daprCli.Annotations.Add(new EndpointAnnotation(ProtocolType.Tcp, name: "grpc", port: sidecarOptions?.DaprGrpcPort));
            daprCli.Annotations.Add(new EndpointAnnotation(ProtocolType.Tcp, name: "http", port: sidecarOptions?.DaprHttpPort));
            daprCli.Annotations.Add(new EndpointAnnotation(ProtocolType.Tcp, name: "metrics", port: sidecarOptions?.MetricsPort));
            if (sidecarOptions?.EnableProfiling == true)
            {
                daprCli.Annotations.Add(new EndpointAnnotation(ProtocolType.Tcp, name: "profile", port: sidecarOptions?.ProfilePort));
            }

            // NOTE: Telemetry is enabled by default.
            if (this._options.EnableTelemetry != false)
            {
                OtlpConfigurationExtensions.AddOtlpEnvironment(daprCli, _configuration, _environment);
            }

            daprCli.Annotations.Add(
                new ExecutableArgsCallbackAnnotation(
                    updatedArgs =>
                    {
                        AllocatedEndpointAnnotation? httpEndPoint = null;
                        if (resource.TryGetAllocatedEndPoints(out var projectEndPoints))
                        {
                            httpEndPoint = projectEndPoints.FirstOrDefault(endPoint => endPoint.Name == "http");

                            if (httpEndPoint is not null && sidecarOptions?.AppPort is null)
                            {
                                updatedArgs.AddRange(daprAppPortArg(httpEndPoint.Port)());
                            }
                        }

                        updatedArgs.AddRange(daprGrpcPortArg($"{{{{- portForServing \"{daprCliResourceName}_grpc\" -}}}}")());
                        updatedArgs.AddRange(daprHttpPortArg($"{{{{- portForServing \"{daprCliResourceName}_http\" -}}}}")());
                        updatedArgs.AddRange(daprMetricsPortArg($"{{{{- portForServing \"{daprCliResourceName}_metrics\" -}}}}")());
                        if (sidecarOptions?.EnableProfiling == true)
                        {
                            updatedArgs.AddRange(daprProfilePortArg($"{{{{- portForServing \"{daprCliResourceName}_profile\" -}}}}")());
                        }
                        if (sidecarOptions?.AppChannelAddress is null && httpEndPoint is not null)
                        {
                            updatedArgs.AddRange(daprAppChannelAddressArg(httpEndPoint.Address)());
                        }
                    }));

            // Apply environment variables to the CLI...
            daprCli.Annotations.AddRange(daprSidecar.Annotations.OfType<EnvironmentCallbackAnnotation>());

            // The CLI is an artifact of a local run, so it should not be published...
            daprCli.Annotations.Add(ManifestPublishingCallbackAnnotation.Ignore);

            daprSidecar.Annotations.Add(
                new ManifestPublishingCallbackAnnotation(
                    context =>
                    {
                        context.Writer.WriteString("type", "dapr.v0");
                        context.Writer.WriteStartObject("dapr");

                        context.Writer.WriteString("application", resource.Name);
                        context.Writer.TryWriteString("appChannelAddress", sidecarOptions?.AppChannelAddress);
                        context.Writer.TryWriteString("appHealthCheckPath", sidecarOptions?.AppHealthCheckPath);
                        context.Writer.TryWriteNumber("appHealthProbeInterval", sidecarOptions?.AppHealthProbeInterval);
                        context.Writer.TryWriteNumber("appHealthProbeTimeout", sidecarOptions?.AppHealthProbeTimeout);
                        context.Writer.TryWriteNumber("appHealthThreshold", sidecarOptions?.AppHealthThreshold);
                        context.Writer.TryWriteString("appId", appId);
                        context.Writer.TryWriteNumber("appMaxConcurrency", sidecarOptions?.AppMaxConcurrency);
                        context.Writer.TryWriteNumber("appPort", sidecarOptions?.AppPort);
                        context.Writer.TryWriteString("appProtocol", sidecarOptions?.AppProtocol);
                        context.Writer.TryWriteStringArray("command", sidecarOptions?.Command);
                        context.Writer.TryWriteStringArray("components", componentReferenceAnnotations.Select(componentReferenceAnnotation => componentReferenceAnnotation.Component.Name));
                        context.Writer.TryWriteString("config", context.GetManifestRelativePath(sidecarOptions?.Config));
                        context.Writer.TryWriteNumber("daprGrpcPort", sidecarOptions?.DaprGrpcPort);
                        context.Writer.TryWriteNumber("daprHttpMaxRequestSize", sidecarOptions?.DaprHttpMaxRequestSize);
                        context.Writer.TryWriteNumber("daprHttpPort", sidecarOptions?.DaprHttpPort);
                        context.Writer.TryWriteNumber("daprHttpReadBufferSize", sidecarOptions?.DaprHttpReadBufferSize);
                        context.Writer.TryWriteNumber("daprInternalGrpcPort", sidecarOptions?.DaprInternalGrpcPort);
                        context.Writer.TryWriteString("daprListenAddresses", sidecarOptions?.DaprListenAddresses);
                        context.Writer.TryWriteBoolean("enableApiLogging", sidecarOptions?.EnableApiLogging);
                        context.Writer.TryWriteBoolean("enableAppHealthCheck", sidecarOptions?.EnableAppHealthCheck);
                        context.Writer.TryWriteString("logLevel", sidecarOptions?.LogLevel);
                        context.Writer.TryWriteNumber("metricsPort", sidecarOptions?.MetricsPort);
                        context.Writer.TryWriteString("placementHostAddress", sidecarOptions?.PlacementHostAddress);
                        context.Writer.TryWriteNumber("profilePort", sidecarOptions?.ProfilePort);
                        context.Writer.TryWriteStringArray("resourcesPath", sidecarOptions?.ResourcesPaths.Select(path => context.GetManifestRelativePath(path)));
                        context.Writer.TryWriteString("runFile", context.GetManifestRelativePath(sidecarOptions?.RunFile));
                        context.Writer.TryWriteString("runtimePath", context.GetManifestRelativePath(sidecarOptions?.RuntimePath));
                        context.Writer.TryWriteString("unixDomainSocket", sidecarOptions?.UnixDomainSocket);

                        context.Writer.WriteEndObject();
                    }));

            sideCars.Add(daprCli);
        }

        appModel.Resources.AddRange(sideCars);
    }

    /// <summary>
    /// Return the first verified dapr path
    /// </summary>
    static string? GetDefaultDaprPath()
    {
        foreach (var path in GetAvailablePaths())
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return default;

        // Return all the possible paths for dapr
        static IEnumerable<string> GetAvailablePaths()
        {
            if (OperatingSystem.IsWindows())
            {
                var pathRoot = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:";

                // Installed windows paths:
                yield return Path.Combine(pathRoot, "dapr", "dapr.exe");

                yield break;
            }

            // Add $HOME/dapr path:
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(homePath, "dapr", "dapr");

            // Linux & MacOS path:
            yield return Path.Combine("/usr", "local", "bin", "dapr");

            // MacOS Homebrew path:
            if (OperatingSystem.IsMacOS() && Environment.GetEnvironmentVariable("HOMEBREW_PREFIX") is string homebrewPrefix)
            {
                yield return Path.Combine(homebrewPrefix, "bin", "dapr");
            }
        }
    }

    public void Dispose()
    {
        if (_onDemandResourcesRootPath is not null)
        {
            _logger.LogInformation("Stopping Dapr-related resources...");

            try
            {
                Directory.Delete(_onDemandResourcesRootPath, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary Dapr resources directory: {OnDemandResourcesRootPath}", _onDemandResourcesRootPath);
            }
        }
    }

    private async Task<IReadOnlyDictionary<string, string>> StartOnDemandDaprComponentsAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var onDemandComponents =
            appModel
                .Resources
                .OfType<DaprComponentResource>()
                .Where(component => component.Options?.LocalPath is null)
                .ToList();

        var onDemandResourcesPaths = new Dictionary<string, string>();

        if (onDemandComponents.Any())
        {
            _logger.LogInformation("Starting Dapr-related resources...");

            _onDemandResourcesRootPath = Directory.CreateTempSubdirectory("aspire-dapr.").FullName;

            foreach (var component in onDemandComponents)
            {
                Func<string, Task<string>> contentWriter =
                    async content =>
                    {
                        string componentDirectory = Path.Combine(_onDemandResourcesRootPath, component.Name);

                        Directory.CreateDirectory(componentDirectory);

                        string componentPath = Path.Combine(componentDirectory, $"{component.Name}.yaml");

                        await File.WriteAllTextAsync(componentPath, content, cancellationToken).ConfigureAwait(false);

                        return componentPath;
                    };

                string componentPath = await (component.Type switch
                {
                    DaprConstants.BuildingBlocks.PubSub => GetPubSubAsync(component, contentWriter, cancellationToken),
                    DaprConstants.BuildingBlocks.StateStore => GetStateStoreAsync(component, contentWriter, cancellationToken),
                    _ => throw new InvalidOperationException($"Unsupported Dapr component type '{component.Type}'.")
                }).ConfigureAwait(false);

                onDemandResourcesPaths.Add(component.Name, componentPath);
            }
        }

        return onDemandResourcesPaths;
    }

    private async Task<string> GetPubSubAsync(DaprComponentResource component, Func<string, Task<string>> contentWriter, CancellationToken cancellationToken)
    {
        string userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string daprDefaultComponentsDirectory = Path.Combine(userDirectory, ".dapr", "components");
        string daprDefaultStateStorePath = Path.Combine(daprDefaultComponentsDirectory, "pubsub.yaml");

        if (File.Exists(daprDefaultStateStorePath))
        {
            _logger.LogInformation("Using default Dapr pub-sub for component '{ComponentName}'.", component.Name);

            string defaultContent = await File.ReadAllTextAsync(daprDefaultStateStorePath, cancellationToken).ConfigureAwait(false);
            string newContent = defaultContent.Replace("name: pubsub", $"name: {component.Name}");

            return await contentWriter(newContent).ConfigureAwait(false);
        }
        else
        {
            _logger.LogInformation("Using in-memory Dapr pub-sub for component '{ComponentName}'.", component.Name);

            return await contentWriter(GetInMemoryPubSubContent(component)).ConfigureAwait(false);
        }
    }

    private async Task<string> GetStateStoreAsync(DaprComponentResource component, Func<string, Task<string>> contentWriter, CancellationToken cancellationToken)
    {
        string userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string daprDefaultComponentsDirectory = Path.Combine(userDirectory, ".dapr", "components");
        string daprDefaultStateStorePath = Path.Combine(daprDefaultComponentsDirectory, "statestore.yaml");

        if (File.Exists(daprDefaultStateStorePath))
        {
            _logger.LogInformation("Using default Dapr state store for component '{ComponentName}'.", component.Name);

            string defaultContent = await File.ReadAllTextAsync(daprDefaultStateStorePath, cancellationToken).ConfigureAwait(false);
            string newContent = defaultContent.Replace("name: statestore", $"name: {component.Name}");

            return await contentWriter(newContent).ConfigureAwait(false);
        }
        else
        {
            _logger.LogInformation("Using in-memory Dapr state store for component '{ComponentName}'.", component.Name);

            return await contentWriter(GetInMemoryStateStoreContent(component)).ConfigureAwait(false);
        }
    }

    private static string GetInMemoryPubSubContent(DaprComponentResource component)
    {
        // NOTE: This component can only be used within a single Dapr application.

        return
            $"""
            apiVersion: dapr.io/v1alpha1
            kind: Component
            metadata:
                name: {component.Name}
            spec:
                type: pubsub.in-memory
                version: v1
                metadata: []
            """;
    }

    private static string GetInMemoryStateStoreContent(DaprComponentResource component)
    {
        return
            $"""
            apiVersion: dapr.io/v1alpha1
            kind: Component
            metadata:
                name: {component.Name}
            spec:
                type: state.in-memory
                version: v1
                metadata: []
            """;
    }
}

internal static class IListExtensions
{
    public static void AddRange<T>(this IList<T> list, IEnumerable<T> collection)
    {
        foreach (var item in collection)
        {
            list.Add(item);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Sockets;
using static Aspire.Hosting.Dapr.CommandLineArgs;

namespace Aspire.Hosting.Dapr;

internal sealed class DaprDistributedApplicationLifecycleHook : IDistributedApplicationLifecycleHook, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DaprDistributedApplicationLifecycleHook> _logger;
    private readonly DaprOptions _options;
    private readonly DaprPortManager _portManager;

    private string? _onDemandResourcesRootPath;

    private static readonly string s_defaultDaprPath =
        OperatingSystem.IsWindows()
            ? Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:", "dapr", "dapr.exe")
            : Path.Combine("/usr", "local", "bin", "dapr");

    private const int DaprHttpPortStartRange = 50001;

    public DaprDistributedApplicationLifecycleHook(IConfiguration configuration, IHostEnvironment environment, ILogger<DaprDistributedApplicationLifecycleHook> logger, DaprOptions options, DaprPortManager portManager)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
        _options = options;
        _portManager = portManager;
    }

    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        string appHostDirectory = _configuration["AppHost:Directory"] ?? throw new InvalidOperationException("Unable to obtain the application host directory.");

        var onDemandResourcesPaths = await StartOnDemandDaprComponentsAsync(appModel, cancellationToken).ConfigureAwait(false);

        var projectResources = appModel.GetProjectResources().ToArray();

        foreach (var project in projectResources)
        {
            if (!project.TryGetLastAnnotation<IServiceMetadata>(out var projectMetadata))
            {
                continue;
            }

            if (!project.TryGetLastAnnotation<DaprSidecarAnnotation>(out var daprAnnotation))
            {
                continue;
            }

            var projectName = Path.GetFileNameWithoutExtension(projectMetadata.ProjectPath);

            var sidecarOptions = daprAnnotation.Options;

            string fileName = this._options.DaprPath ?? s_defaultDaprPath;
            string workingDirectory = Path.GetDirectoryName(projectMetadata.ProjectPath)!;

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

            var componentReferenceAnnotations = project.Annotations.OfType<DaprComponentReferenceAnnotation>();

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

            var daprAppPortArg = (int? port) => NamedArg("--app-port", port);
            var daprGrpcPortArg = (int? port) => NamedArg("--dapr-grpc-port", port);
            var daprHttpPortArg = (int? port) => NamedArg("--dapr-http-port", port);
            var daprMetricsPortArg = (int? port) => NamedArg("--metrics-port", port);
            var daprProfilePortArg = (int? port) => NamedArg("--profile-port", port);

            var daprCommandLine =
                CommandLineBuilder
                    .Create(
                        fileName,
                        Command("run"),
                        daprAppPortArg(sidecarOptions?.AppPort),
                        daprGrpcPortArg(sidecarOptions?.DaprGrpcPort),
                        daprHttpPortArg(sidecarOptions?.DaprHttpPort),
                        daprMetricsPortArg(sidecarOptions?.MetricsPort),
                        daprProfilePortArg(sidecarOptions?.ProfilePort),
                        NamedArg("--app-channel-address", sidecarOptions?.AppChannelAddress),
                        NamedArg("--app-health-check-path", sidecarOptions?.AppHealthCheckPath),
                        NamedArg("--app-health-probe-interval", sidecarOptions?.AppHealthProbeInterval),
                        NamedArg("--app-health-probe-timeout", sidecarOptions?.AppHealthProbeTimeout),
                        NamedArg("--app-health-threshold", sidecarOptions?.AppHealthThreshold),
                        NamedArg("--app-id", sidecarOptions?.AppId),
                        NamedArg("--app-max-concurrency", sidecarOptions?.AppMaxConcurrency),
                        NamedArg("--app-protocol", sidecarOptions?.AppProtocol),
                        NamedArg("--config", NormalizePath(sidecarOptions?.Config)),
                        NamedArg("--dapr-http-max-request-size", sidecarOptions?.DaprHttpMaxRequestSize),
                        NamedArg("--dapr-http-read-buffer-size", sidecarOptions?.DaprHttpReadBufferSize),
                        NamedArg("--dapr-internal-grpc-port", sidecarOptions?.DaprInternalGrpcPort),
                        NamedArg("--dapr-listen-addresses", sidecarOptions?.DaprListenAddresses),
                        NamedArg("--enable-api-logging", sidecarOptions?.EnableApiLogging),
                        NamedArg("--enable-app-health-check", sidecarOptions?.EnableAppHealthCheck),
                        NamedArg("--enable-profiling", sidecarOptions?.EnableProfiling),
                        NamedArg("--log-level", sidecarOptions?.LogLevel),
                        NamedArg("--placement-host-address", sidecarOptions?.PlacementHostAddress),
                        NamedArg("--resources-path", aggregateResourcesPaths),
                        NamedArg("--run-file", NormalizePath(sidecarOptions?.RunFile)),
                        NamedArg("--unix-domain-socket", sidecarOptions?.UnixDomainSocket),
                        PostOptionsArgs(Args(sidecarOptions?.Command)));

            //
            // NOTE: Use custom port allocator for unspecified ports until DCP supports executable command line port templates.
            //

            Dictionary<string, (int Port, Func<int?, CommandLineArgBuilder> ArgsBuilder, string? PortEnvVar)> ports = new()
            {
                { "grpc", (sidecarOptions?.DaprGrpcPort ?? this._portManager.ReservePort(DaprHttpPortStartRange), daprGrpcPortArg, "DAPR_GRPC_PORT") },
                { "http", (sidecarOptions?.DaprHttpPort ?? this._portManager.ReservePort(DaprHttpPortStartRange), daprHttpPortArg, "DAPR_HTTP_PORT") },
                { "metrics", (sidecarOptions?.MetricsPort ?? this._portManager.ReservePort(DaprHttpPortStartRange), daprMetricsPortArg, null) }
            };

            if (sidecarOptions?.EnableProfiling == true)
            {
                ports.Add("profile", (sidecarOptions?.ProfilePort ?? this._portManager.ReservePort(DaprHttpPortStartRange), daprProfilePortArg, null));
            }

            if (!(sidecarOptions?.AppId is { } appId))
            {
                throw new DistributedApplicationException("AppId is required for Dapr sidecar executable.");
            }

            var resource = new ExecutableResource(appId, fileName, workingDirectory, daprCommandLine.Arguments.ToArray());

            project.Annotations.Add(
                new EnvironmentCallbackAnnotation(
                    env =>
                    {
                        if (resource.TryGetAllocatedEndPoints(out var endPoints))
                        {
                            foreach (var endPoint in endPoints)
                            {
                                if (ports.TryGetValue(endPoint.Name, out var value) && value.PortEnvVar is not null)
                                {
                                    env.TryAdd(value.PortEnvVar, endPoint.Port.ToString(CultureInfo.InvariantCulture));
                                }
                            }
                        }
                    }));

            resource.Annotations.AddRange(ports.Select(port => new ServiceBindingAnnotation(ProtocolType.Tcp, name: port.Key, port: port.Value.Port)));

            // NOTE: Telemetry is enabled by default.
            if (this._options.EnableTelemetry != false)
            {
                OtlpConfigurationExtensions.AddOtlpEnvironment(resource, _configuration, _environment);
            }

            resource.Annotations.Add(
                new ExecutableArgsCallbackAnnotation(
                    updatedArgs =>
                    {
                        if (project.TryGetAllocatedEndPoints(out var projectEndPoints))
                        {
                            var httpEndPoint = projectEndPoints.FirstOrDefault(endPoint => endPoint.Name == "http");

                            if (httpEndPoint is not null && sidecarOptions?.AppPort is null)
                            {
                                updatedArgs.AddRange(daprAppPortArg(httpEndPoint.Port)());
                            }
                        }

                        foreach (var port in ports)
                        {
                            updatedArgs.AddRange(port.Value.ArgsBuilder(port.Value.Port)());
                        }
                    }));

            resource.Annotations.Add(
                new ManifestPublishingCallbackAnnotation(
                    context =>
                    {
                        context.Writer.WriteString("type", "dapr.v0");
                        context.Writer.WriteStartObject("dapr");

                        context.Writer.WriteString("application", project.Name);
                        context.Writer.TryWriteString("appChannelAddress", sidecarOptions?.AppChannelAddress);
                        context.Writer.TryWriteString("appHealthCheckPath", sidecarOptions?.AppHealthCheckPath);
                        context.Writer.TryWriteNumber("appHealthProbeInterval", sidecarOptions?.AppHealthProbeInterval);
                        context.Writer.TryWriteNumber("appHealthProbeTimeout", sidecarOptions?.AppHealthProbeTimeout);
                        context.Writer.TryWriteNumber("appHealthThreshold", sidecarOptions?.AppHealthThreshold);
                        context.Writer.TryWriteString("appId", sidecarOptions?.AppId);
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
                        context.Writer.TryWriteString("unixDomainSocket", sidecarOptions?.UnixDomainSocket);

                        context.Writer.WriteEndObject();
                    }));

            appModel.Resources.Add(resource);
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
            _logger.LogInformation($"Using default Dapr pub-sub for component '{component.Name}'.");

            string defaultContent = await File.ReadAllTextAsync(daprDefaultStateStorePath, cancellationToken).ConfigureAwait(false);
            string newContent = defaultContent.Replace("name: pubsub", $"name: {component.Name}");

            return await contentWriter(newContent).ConfigureAwait(false);
        }
        else
        {
            _logger.LogInformation($"Using in-memory Dapr pub-sub for component '{component.Name}'.");

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
            _logger.LogInformation($"Using default Dapr state store for component '{component.Name}'.");

            string defaultContent = await File.ReadAllTextAsync(daprDefaultStateStorePath, cancellationToken).ConfigureAwait(false);
            string newContent = defaultContent.Replace("name: statestore", $"name: {component.Name}");

            return await contentWriter(newContent).ConfigureAwait(false);
        }
        else
        {
            _logger.LogInformation($"Using in-memory Dapr state store for component '{component.Name}'.");

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

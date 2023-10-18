// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Net.Sockets;
using static Aspire.Hosting.Dapr.CommandLineArgs;

namespace Aspire.Hosting.Dapr;

internal sealed class DaprDistributedApplicationLifecycleHook : IDistributedApplicationLifecycleHook
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly DaprOptions _options;
    private readonly DaprPortManager _portManager;

    private static readonly string s_defaultDaprPath =
        OperatingSystem.IsWindows()
            ? Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:", "dapr", "dapr.exe")
            : Path.Combine("/usr", "local", "bin", "dapr");

    private const int DaprHttpPortStartRange = 50001;

    public DaprDistributedApplicationLifecycleHook(IConfiguration configuration, IHostEnvironment environment, DaprOptions options, DaprPortManager portManager)
    {
        _configuration = configuration;
        _environment = environment;
        this._options = options;
        this._portManager = portManager;
    }

    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
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
                        NamedArg("--config", sidecarOptions?.Config),
                        NamedArg("--dapr-http-max-request-size", sidecarOptions?.DaprHttpMaxRequestSize),
                        NamedArg("--dapr-http-read-buffer-size", sidecarOptions?.DaprHttpReadBufferSize),
                        NamedArg("--dapr-internal-grpc-port", sidecarOptions?.DaprInternalGrpcPort),
                        NamedArg("--dapr-listen-addresses", sidecarOptions?.DaprListenAddresses),
                        NamedArg("--enable-api-logging", sidecarOptions?.EnableApiLogging),
                        NamedArg("--enable-app-health-check", sidecarOptions?.EnableAppHealthCheck),
                        NamedArg("--enable-profiling", sidecarOptions?.EnableProfiling),
                        NamedArg("--log-level", sidecarOptions?.LogLevel),
                        NamedArg("--placement-host-address", sidecarOptions?.PlacementHostAddress),
                        NamedArg("--resources-path", sidecarOptions?.ResourcesPaths),
                        NamedArg("--run-file", sidecarOptions?.RunFile),
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

            appModel.Resources.Add(resource);
        }

        return Task.CompletedTask;
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

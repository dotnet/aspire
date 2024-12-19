// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dapr.Models.ComponentSpec;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static Aspire.Hosting.Dapr.CommandLineArgs;
using static Aspire.Hosting.Dapr.DaprConstants;

namespace Aspire.Hosting.Dapr;

internal sealed class DaprDistributedApplicationLifecycleHook : IDistributedApplicationLifecycleHook, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DaprDistributedApplicationLifecycleHook> _logger;
    private readonly DaprOptions _options;
    private readonly ResourceNotificationService _resourceNotificationService;

    private readonly string _onDemandResourcesRootPath;

    public DaprDistributedApplicationLifecycleHook(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<DaprDistributedApplicationLifecycleHook> logger,
        IOptions<DaprOptions> options,
        ResourceNotificationService resourceNotificationService)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
        _options = options.Value;
        _resourceNotificationService = resourceNotificationService;
        _onDemandResourcesRootPath = Directory.CreateTempSubdirectory("aspire-dapr.").FullName;
    }

    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        string appHostDirectory = _configuration["AppHost:Directory"] ?? throw new InvalidOperationException("Unable to obtain the application host directory.");

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

            var waitAnnotationsToCopyToDaprCli = new List<WaitAnnotation>();

            foreach (var componentReferenceAnnotation in componentReferenceAnnotations)
            {
                // Wait for components itself to be ready.
                waitAnnotationsToCopyToDaprCli.Add(new WaitAnnotation(componentReferenceAnnotation.Component, WaitType.WaitUntilHealthy));
                // Whilst we are passing over each component annotations collect the list of annotations to copy to the Dapr CLI.
                if (componentReferenceAnnotation.Component.TryGetAnnotationsOfType<WaitAnnotation>(out var componentWaitAnnotations))
                {
                    waitAnnotationsToCopyToDaprCli.AddRange(componentWaitAnnotations);
                }
                aggregateResourcesPaths.Add(GetComponentPath(componentReferenceAnnotation.Component.Name));
            }

            // It is possible that we have duplicate wate annotations so we just dedupe them here.
            var distinctWaitAnnotationsToCopyToDaprCli = waitAnnotationsToCopyToDaprCli.DistinctBy(w => (w.Resource, w.WaitType));

            var daprAppPortArg = (int? port) => ModelNamedArg("--app-port", port);
            var daprGrpcPortArg = (object port) => ModelNamedObjectArg("--dapr-grpc-port", port);
            var daprHttpPortArg = (object port) => ModelNamedObjectArg("--dapr-http-port", port);
            var daprMetricsPortArg = (object port) => ModelNamedObjectArg("--metrics-port", port);
            var daprProfilePortArg = (object port) => ModelNamedObjectArg("--profile-port", port);
            var daprAppChannelAddressArg = (string? address) => ModelNamedArg("--app-channel-address", address);
            var daprAppProtocol = (string? protocol) => ModelNamedArg("--app-protocol", protocol);

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
                        ModelNamedArg("--scheduler-host-address", sidecarOptions?.SchedulerHostAddress),
                        ModelNamedArg("--unix-domain-socket", sidecarOptions?.UnixDomainSocket),
                        PostOptionsArgs(Args(sidecarOptions?.Command)));

            var daprCliResourceName = $"{daprSidecar.Name}-cli";
            var daprCli = new ExecutableResource(daprCliResourceName, fileName, appHostDirectory);

            // Add all the unique wait annotations to the CLI.
            daprCli.Annotations.AddRange(distinctWaitAnnotationsToCopyToDaprCli);

            resource.Annotations.Add(
                new EnvironmentCallbackAnnotation(
                    context =>
                    {
                        if (context.ExecutionContext.IsPublishMode)
                        {
                            return;
                        }

                        var http = daprCli.GetEndpoint("http");
                        var grpc = daprCli.GetEndpoint("grpc");

                        context.EnvironmentVariables.TryAdd("DAPR_HTTP_PORT", http.Port.ToString(CultureInfo.InvariantCulture));
                        context.EnvironmentVariables.TryAdd("DAPR_GRPC_PORT", grpc.Port.ToString(CultureInfo.InvariantCulture));

                        context.EnvironmentVariables.TryAdd("DAPR_GRPC_ENDPOINT", grpc);
                        context.EnvironmentVariables.TryAdd("DAPR_HTTP_ENDPOINT", http);
                    }));

            daprCli.Annotations.Add(new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "grpc", port: sidecarOptions?.DaprGrpcPort));
            daprCli.Annotations.Add(new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http", port: sidecarOptions?.DaprHttpPort));
            daprCli.Annotations.Add(new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "metrics", port: sidecarOptions?.MetricsPort));
            if (sidecarOptions?.EnableProfiling == true)
            {
                daprCli.Annotations.Add(new EndpointAnnotation(ProtocolType.Tcp, name: "profile", port: sidecarOptions?.ProfilePort, uriScheme: "http"));
            }

            // NOTE: Telemetry is enabled by default.
            if (this._options.EnableTelemetry != false)
            {
                OtlpConfigurationExtensions.AddOtlpEnvironment(daprCli, _configuration, _environment);
            }

            daprCli.Annotations.Add(
                new CommandLineArgsCallbackAnnotation(
                    updatedArgs =>
                    {
                        updatedArgs.AddRange(daprCommandLine.Arguments);
                        var endPoint = GetEndpointReference(sidecarOptions, resource);

                        if (sidecarOptions?.AppPort is null && endPoint is { appEndpoint.IsAllocated: true })
                        {
                            updatedArgs.AddRange(daprAppPortArg(endPoint.Value.appEndpoint.Port)());
                        }

                        var grpc = daprCli.GetEndpoint("grpc");
                        var http = daprCli.GetEndpoint("http");
                        var metrics = daprCli.GetEndpoint("metrics");

                        updatedArgs.AddRange(daprGrpcPortArg(grpc.Property(EndpointProperty.TargetPort))());
                        updatedArgs.AddRange(daprHttpPortArg(http.Property(EndpointProperty.TargetPort))());
                        updatedArgs.AddRange(daprMetricsPortArg(metrics.Property(EndpointProperty.TargetPort))());

                        if (sidecarOptions?.EnableProfiling == true)
                        {
                            var profiling = daprCli.GetEndpoint("profiling");

                            updatedArgs.AddRange(daprProfilePortArg(profiling.Property(EndpointProperty.TargetPort))());
                        }

                        if (sidecarOptions?.AppChannelAddress is null && endPoint is { appEndpoint.IsAllocated: true })
                        {
                            updatedArgs.AddRange(daprAppChannelAddressArg(endPoint.Value.appEndpoint.Host)());
                        }
                        if (sidecarOptions?.AppProtocol is null && endPoint is { appEndpoint.IsAllocated: true })
                        {
                            updatedArgs.AddRange(daprAppProtocol(endPoint.Value.protocol)());
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
                        context.Writer.TryWriteStringArray("resourcesPath", sidecarOptions?.ResourcesPaths.Select(path => context.GetManifestRelativePath(path + "/")));
                        context.Writer.TryWriteString("runFile", context.GetManifestRelativePath(sidecarOptions?.RunFile));
                        context.Writer.TryWriteString("runtimePath", context.GetManifestRelativePath(sidecarOptions?.RuntimePath));
                        context.Writer.TryWriteString("schedulerHostAddress", sidecarOptions?.SchedulerHostAddress);
                        context.Writer.TryWriteString("unixDomainSocket", sidecarOptions?.UnixDomainSocket);

                        context.Writer.WriteEndObject();
                    }));

            sideCars.Add(daprCli);
        }

        appModel.Resources.AddRange(sideCars);
        return Task.CompletedTask;
    }

    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        StartOnDemandDaprComponentsAsync(appModel, cancellationToken);
        return Task.CompletedTask;
    }

    // This method resolves the application's endpoint and the protocol that the dapr side car will use.
    // It depends on DaprSidecarOptions.AppProtocol and DaprSidecarOptions.AppEndpoint.
    // - If both are null default to 'http' for both.
    // - If AppProtocol is not null try to get an endpoint with the name of the protocol.
    // - if AppEndpoint is not null try to use the scheme as the protocol.
    // - if both are not null just use both options.
    static (EndpointReference appEndpoint, string protocol)? GetEndpointReference(DaprSidecarOptions? sidecarOptions, IResource resource)
    {
        if (resource is IResourceWithEndpoints resourceWithEndpoints)
        {
            return (sidecarOptions?.AppProtocol, sidecarOptions?.AppEndpoint) switch
            {
                (null, null) => (resourceWithEndpoints.GetEndpoint("http"), "http"),
                (null, string appEndpoint) => (resourceWithEndpoints.GetEndpoint(appEndpoint), resourceWithEndpoints.GetEndpoint(appEndpoint).Scheme),
                (string appProtocol, null) => (resourceWithEndpoints.GetEndpoint(appProtocol), appProtocol),
                (string appProtocol, string appEndpoint) => (resourceWithEndpoints.GetEndpoint(appEndpoint), appProtocol)
            };
        }
        return null;
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
            yield return "/usr/local/bin/dapr";

            // Arch Linux path:
            yield return "/usr/bin/dapr";

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

    private void StartOnDemandDaprComponentsAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var onDemandComponents =
            appModel
                .Resources
                .OfType<DaprComponentResource>()
                // .Where(component => component.Options?.LocalPath is null)
                .ToList();

        var onDemandResourcesPaths = new Dictionary<string, string>();

        if (onDemandComponents.Any())
        {
            _logger.LogInformation("Starting Dapr-related resources...");

            foreach (var component in onDemandComponents)
            {
                // we don't await this task, because we want to wait in the background for all dependencies to be ready
                _ = StartComponent(onDemandResourcesPaths, component, cancellationToken);
            }
        }
    }

    private async Task StartComponent(Dictionary<string, string> onDemandResourcesPaths, DaprComponentResource component, CancellationToken cancellationToken)
    {
        var resourcePath = await CreateComponentSpec(component, cancellationToken).ConfigureAwait(false);
        onDemandResourcesPaths.Add(resourcePath.Item1, resourcePath.Item2);

        await _resourceNotificationService.WaitForDependenciesAsync(component, cancellationToken).ConfigureAwait(false);
        if (component.TryGetAnnotationsOfType<WaitAnnotation>(out var waitAnnotations))
        {
            foreach (var waitAnnotation in waitAnnotations)
            {
                await _resourceNotificationService.WaitForResourceHealthyAsync(waitAnnotation.Resource.Name, cancellationToken).ConfigureAwait(false);
            }
        }
        await _resourceNotificationService.PublishUpdateAsync(component, s => s with
        {
            State = KnownResourceStates.Running
        }).ConfigureAwait(false);
    }

    private async Task<(string, string)> CreateComponentSpec(DaprComponentResource component, CancellationToken cancellationToken)
    {
        var refType = DaprSupportedRefTypes.Where(r => r.Type == component.Type).SingleOrDefault();
        var spec = new ComponentSpec
        {
            Metadata = new Metadata
            {
                Name = component.Name
            },
            Spec = new Spec
            {
                Type = component.Type,
                Version = refType?.Version ?? "v1",
                Metadata = component.Options?.Configuration ?? new List<MetadataValue>()
            },
            Auth = component.Options?.SecretStore is not null ?
                new()
                {
                    SecretStore = component.Options.SecretStore.Name
                }
                : null
        };

        // analyze the component references, and lookup if there is a reference with a connection string
        if (component.TryGetAnnotationsOfType<ResourceRelationshipAnnotation>(out var resourceRelationshipAnnotations))
        {
            var resources = resourceRelationshipAnnotations.Where(r => r.Resource is IResourceWithConnectionString && r.Type == "Reference");
            if (resources.Count() > 1)
            {
                throw new InvalidOperationException($"The component '{component.Name}' has more than one resource with a connection string.");
            }
            var reference = (IResourceWithConnectionString)resources.Single().Resource;

            var connectionString = await reference.GetValueAsync(cancellationToken).ConfigureAwait(false);
            if (connectionString is null)
            {
                throw new InvalidOperationException($"The resource '{reference.Name}' does not have a connection string.");
            }
            if (refType?.propertyMapping is not null)
            {
                spec.Spec.Metadata.AddRange(refType.propertyMapping(connectionString));
            }
            else
            {
                throw new InvalidOperationException($"The component '{component.Name}' does not support connection strings.");
            }
        }

        string componentDirectory = GetComponentPath(component.Name);

        Directory.CreateDirectory(componentDirectory);

        string componentPath = Path.Combine(componentDirectory, $"{component.Name}.yaml");
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)

            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();
        serializer.Serialize(File.CreateText(componentPath), spec);
        var content = serializer.Serialize(spec);
        await File.WriteAllTextAsync(componentPath, content, cancellationToken).ConfigureAwait(false);
        return (component.Name, componentPath);

    }

    private string GetComponentPath(string componantName)
    {
        return Path.Combine(_onDemandResourcesRootPath, componantName);
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

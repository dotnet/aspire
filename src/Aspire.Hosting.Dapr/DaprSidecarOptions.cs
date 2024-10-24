// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Aspire.Hosting.Dapr;

/// <summary>
/// Options for configuring a Dapr sidecar.
/// </summary>
public sealed record DaprSidecarOptions
{
    /// <summary>
    /// Gets or sets the network address at which the application listens.
    /// </summary>
    public string? AppChannelAddress { get; init; }

    /// <summary>
    /// Gets or sets the path used for health checks (HTTP only).
    /// </summary>
    public string? AppHealthCheckPath { get; init; }

    /// <summary>
    ///  Gets or sets the interval, in seconds, to probe for the health of the application.
    /// </summary>
    public int? AppHealthProbeInterval { get; init; }

    /// <summary>
    ///  Gets or sets the timeout, in milliseconds, for application health probes.
    /// </summary>
    public int? AppHealthProbeTimeout { get; init; }

    /// <summary>
    /// Gets or sets the number of consecutive failures for the application to be considered unhealthy.
    /// </summary>
    public int? AppHealthThreshold { get; init; }

    /// <summary>
    /// Gets or sets the ID for the application, used for service discovery.
    /// </summary>
    public string? AppId { get; init; }

    /// <summary>
    /// Gets or sets the concurrency level of the application (unlimited if omitted).
    /// </summary>
    public int? AppMaxConcurrency { get; init; }

    /// <summary>
    /// Gets or sets the port on which the application is listening.
    /// </summary>
    public int? AppPort { get; init; }

    /// <summary>
    /// Gets or sets the protocol (i.e. grpc, grpcs, http, https, h2c) the Dapr sidecar uses to talk to the application.
    /// </summary>
    public string? AppProtocol { get; init; }

    /// <summary>
    /// Gets or sets the endpoint of the application the sidecar is connected to.
    /// </summary>
    public string? AppEndpoint { get; init; }

    /// <summary>
    /// Gets or sets the command run by the Dapr CLI as part of starting the sidecar.
    /// </summary>
    public IImmutableList<string> Command { get; init; } = ImmutableList<string>.Empty;

    /// <summary>
    /// Gets or sets the path to the Dapr sidecar configuration file.
    /// </summary>
    public string? Config { get; init; }

    /// <summary>
    /// Gets or sets the gRPC port on which the Dapr sidecar should listen.
    /// </summary>
    public int? DaprGrpcPort { get; init; }

    /// <summary>
    /// Gets or sets the maximum size, in MB, of a Dapr request body.
    /// </summary>
    public int? DaprHttpMaxRequestSize { get; init; }

    /// <summary>
    ///  Gets or sets the HTTP port on which the Dapr sidecard should listen.
    /// </summary>
    public int? DaprHttpPort { get; init; }

    /// <summary>
    /// Gets or sets the maximum size, in KB, of the HTTP header read buffer.
    /// </summary>
    public int? DaprHttpReadBufferSize { get; init; }

    /// <summary>
    /// Gets or sets the gRPC port on which the Dapr sidecar should listen for sidecar-to-sidecar calls.
    /// </summary>
    public int? DaprInternalGrpcPort { get; init; }

    /// <summary>
    /// Gets or sets a comma (,) delimited list of IP addresses at which the Dapr sidecar will listen.
    /// </summary>
    public string? DaprListenAddresses { get; init; }

    /// <summary>
    /// Gets or sets whether the Dapr sidecar logs API calls at INFO verbosity.
    /// </summary>
    public bool? EnableApiLogging { get; init; }

    /// <summary>
    /// Gets or sets whether health checks are performed for the application.
    /// </summary>
    public bool? EnableAppHealthCheck { get; init; }

    /// <summary>
    /// Gets or sets whether to perform pprof profiling via the application HTTP endpoint.
    /// </summary>
    public bool? EnableProfiling { get; init; }

    /// <summary>
    /// Gets or sets the Dapr sidecar log verbosity (i.e. debug, info, warn, error, fatal, or panic).
    /// </summary>
    /// <remarks>
    /// The default log verbosity is "info".
    /// </remarks>
    public string? LogLevel { get; init; }

    /// <summary>
    /// Gets or sets the port on which the Dapr sidecar reports metrics.
    /// </summary>
    public int? MetricsPort { get; init; }

    /// <summary>
    /// Gets or sets the address of the placement service.
    /// </summary>
    /// <remarks>
    /// The format is either "hostname" for the default port or "hostname:port" for a custom port.
    /// The default is "localhost".
    /// </remarks>
    public string? PlacementHostAddress { get; init; }

    /// <summary>
    /// Gets or sets the port on which the Dapr sidecar reports profiling data.
    /// </summary>
    public int? ProfilePort { get; init; }

    /// <summary>
    /// Gets or sets the paths of Dapr sidecar resources (i.e. resources).
    /// </summary>
    public IImmutableSet<string> ResourcesPaths { get; init; } = ImmutableHashSet<string>.Empty;

    /// <summary>
    /// Gets or sets the path to the Dapr run file to run.
    /// </summary>
    public string? RunFile { get; init; }

    /// <summary>
    /// Gets or sets the directory of the Dapr runtime (i.e. daprd).
    /// </summary>
    public string? RuntimePath { get; init; }

    /// <summary>
    /// Gets or sets the address of the scheduler service.
    /// </summary>
    /// <remarks>
    /// The format is either "hostname" for the default port or "hostname:port" for a custom port.
    /// The default is "localhost".
    /// </remarks>
    public string? SchedulerHostAddress { get; init; }

    /// <summary>
    /// Gets or sets the path to a Unix Domain Socket (UDS) directory.
    /// </summary>
    /// <remarks>
    /// If specified, the Dapr sidecar will use Unix Domain Sockets for API calls.
    /// </remarks>
    public string? UnixDomainSocket { get; init; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Dcp;

/// <summary>
/// Client for communicating with DCP's Kubernetes-style API.
/// </summary>
internal interface IDcpClient
{
    /// <summary>
    /// Connects to DCP using the specified kubeconfig file.
    /// </summary>
    Task ConnectAsync(string kubeconfigPath, CancellationToken cancellationToken);

    /// <summary>
    /// Creates an Executable resource in DCP.
    /// </summary>
    Task<DcpExecutableResource> CreateExecutableAsync(DcpExecutableSpec spec, CancellationToken cancellationToken);

    /// <summary>
    /// Gets an Executable resource by name.
    /// </summary>
    Task<DcpExecutableResource?> GetExecutableAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an Executable resource by name.
    /// </summary>
    Task DeleteExecutableAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Watches an Executable resource for state changes.
    /// </summary>
    IAsyncEnumerable<DcpExecutableResource> WatchExecutableAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a Service resource in DCP for endpoint allocation.
    /// </summary>
    Task<DcpServiceResource> CreateServiceAsync(DcpServiceSpec spec, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a Service resource by name.
    /// </summary>
    Task<DcpServiceResource?> GetServiceAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Watches a Service resource for state changes.
    /// </summary>
    IAsyncEnumerable<DcpServiceResource> WatchServiceAsync(string name, CancellationToken cancellationToken);
}

/// <summary>
/// Specification for creating a Service in DCP.
/// </summary>
internal sealed record DcpServiceSpec(
    string Name,
    int? Port = null,
    string? Address = null,
    string Protocol = "TCP",
    string AddressAllocationMode = "Localhost");

/// <summary>
/// Represents a Service resource returned from DCP.
/// </summary>
internal sealed record DcpServiceResource(
    string Name,
    string? State,
    string? EffectiveAddress,
    int? EffectivePort);

/// <summary>
/// Specification for creating an Executable in DCP.
/// </summary>
internal sealed record DcpExecutableSpec(
    string Name,
    string ExecutablePath,
    string? WorkingDirectory = null,
    IReadOnlyList<string>? Args = null,
    IReadOnlyDictionary<string, string>? Env = null);

/// <summary>
/// Represents an Executable resource returned from DCP.
/// </summary>
internal sealed record DcpExecutableResource(
    string Name,
    string? State,
    int? Pid,
    string? StdOutFile,
    string? StdErrFile);

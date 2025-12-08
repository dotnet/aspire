// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Aspire.Cli.Backchannel;

/// <summary>
/// Represents a connection to an AppHost instance via the auxiliary backchannel.
/// Encapsulates connection management and RPC method calls.
/// </summary>
internal sealed class AppHostAuxiliaryBackchannel : IDisposable
{
    private readonly ILogger? _logger;
    private JsonRpc? _rpc;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppHostAuxiliaryBackchannel"/> class
    /// for an existing connection.
    /// </summary>
    /// <param name="hash">The hash identifier for this AppHost instance.</param>
    /// <param name="socketPath">The socket path for this connection.</param>
    /// <param name="rpc">The JSON-RPC proxy for communicating with the AppHost.</param>
    /// <param name="mcpInfo">The MCP connection information for the Dashboard.</param>
    /// <param name="appHostInfo">The AppHost information.</param>
    /// <param name="isInScope">Whether this AppHost is within the scope of the MCP server's working directory.</param>
    /// <param name="logger">Optional logger for diagnostic messages.</param>
    public AppHostAuxiliaryBackchannel(
        string hash,
        string socketPath,
        JsonRpc rpc,
        DashboardMcpConnectionInfo? mcpInfo,
        AppHostInformation? appHostInfo,
        bool isInScope,
        ILogger? logger = null)
    {
        Hash = hash;
        SocketPath = socketPath;
        _rpc = rpc;
        McpInfo = mcpInfo;
        AppHostInfo = appHostInfo;
        IsInScope = isInScope;
        ConnectedAt = DateTimeOffset.UtcNow;
        _logger = logger;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppHostAuxiliaryBackchannel"/> class
    /// for a new connection that needs to be established.
    /// </summary>
    /// <param name="socketPath">The socket path to connect to.</param>
    /// <param name="logger">Optional logger for diagnostic messages.</param>
    private AppHostAuxiliaryBackchannel(string socketPath, ILogger? logger = null)
    {
        SocketPath = socketPath;
        Hash = string.Empty;
        ConnectedAt = DateTimeOffset.UtcNow;
        _logger = logger;
    }

    /// <summary>
    /// Gets the hash identifier for this AppHost instance.
    /// </summary>
    public string Hash { get; private set; }

    /// <summary>
    /// Gets the socket path for this connection.
    /// </summary>
    public string SocketPath { get; }

    /// <summary>
    /// Gets the MCP connection information for the Dashboard.
    /// </summary>
    public DashboardMcpConnectionInfo? McpInfo { get; private set; }

    /// <summary>
    /// Gets the AppHost information.
    /// </summary>
    public AppHostInformation? AppHostInfo { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this AppHost is within the scope of the MCP server's working directory.
    /// </summary>
    public bool IsInScope { get; private set; }

    /// <summary>
    /// Gets the timestamp when this connection was established.
    /// </summary>
    public DateTimeOffset ConnectedAt { get; }

    /// <summary>
    /// Gets the JSON-RPC proxy for communicating with the AppHost.
    /// </summary>
    internal JsonRpc? Rpc => _rpc;

    /// <summary>
    /// Creates and connects a new auxiliary backchannel to the specified socket path.
    /// </summary>
    /// <param name="socketPath">The path to the Unix domain socket.</param>
    /// <param name="logger">Optional logger for diagnostic messages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A connected AppHostAuxiliaryBackchannel instance.</returns>
    public static async Task<AppHostAuxiliaryBackchannel> ConnectAsync(
        string socketPath,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var backchannel = new AppHostAuxiliaryBackchannel(socketPath, logger);
        await backchannel.ConnectInternalAsync(cancellationToken).ConfigureAwait(false);
        return backchannel;
    }

    private async Task ConnectInternalAsync(CancellationToken cancellationToken)
    {
        _logger?.LogDebug("Connecting to auxiliary backchannel at {SocketPath}", SocketPath);

        // Connect to the Unix socket
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(SocketPath);
        
        await socket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);

        // Create JSON-RPC connection with proper formatter
        var stream = new NetworkStream(socket, ownsSocket: true);
        _rpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, BackchannelJsonSerializerContext.CreateRpcMessageFormatter()));
        _rpc.StartListening();

        _logger?.LogDebug("Connected to auxiliary backchannel at {SocketPath}", SocketPath);

        // Get the AppHost information
        AppHostInfo = await GetAppHostInformationAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the AppHost information including process IDs and path.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The AppHost information, or null if unavailable.</returns>
    public async Task<AppHostInformation?> GetAppHostInformationAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_rpc is null)
        {
            throw new InvalidOperationException("Not connected to auxiliary backchannel.");
        }

        _logger?.LogDebug("Requesting AppHost information");

        var appHostInfo = await _rpc.InvokeWithCancellationAsync<AppHostInformation?>(
            "GetAppHostInformationAsync",
            [],
            cancellationToken).ConfigureAwait(false);

        return appHostInfo;
    }

    /// <summary>
    /// Requests the AppHost to stop gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the stop request has been sent.</returns>
    public async Task StopAppHostAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_rpc is null)
        {
            throw new InvalidOperationException("Not connected to auxiliary backchannel.");
        }

        _logger?.LogDebug("Requesting AppHost to stop");

        await _rpc.InvokeWithCancellationAsync(
            "StopAppHostAsync",
            [],
            cancellationToken).ConfigureAwait(false);

        _logger?.LogDebug("Stop request sent to AppHost");
    }

    /// <summary>
    /// Gets the Dashboard MCP connection information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The MCP connection information, or null if unavailable.</returns>
    public async Task<DashboardMcpConnectionInfo?> GetDashboardMcpConnectionInfoAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_rpc is null)
        {
            throw new InvalidOperationException("Not connected to auxiliary backchannel.");
        }

        _logger?.LogDebug("Requesting Dashboard MCP connection info");

        var mcpInfo = await _rpc.InvokeWithCancellationAsync<DashboardMcpConnectionInfo?>(
            "GetDashboardMcpConnectionInfoAsync",
            [],
            cancellationToken).ConfigureAwait(false);

        return mcpInfo;
    }

    /// <summary>
    /// Disposes the auxiliary backchannel connection.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _rpc?.Dispose();
        _rpc = null;
    }
}

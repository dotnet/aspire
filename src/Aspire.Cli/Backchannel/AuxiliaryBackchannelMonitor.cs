// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Net.Sockets;
using Aspire.Cli.Commands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Aspire.Cli.Backchannel;

/// <summary>
/// Background service that monitors the auxiliary backchannel directory and maintains
/// connections to all running AppHost instances.
/// </summary>
internal sealed class AuxiliaryBackchannelMonitor(
    ILogger<AuxiliaryBackchannelMonitor> logger,
    CliExecutionContext executionContext) : BackgroundService, IAuxiliaryBackchannelMonitor
{
    private readonly ConcurrentDictionary<string, AppHostConnection> _connections = new();
    private readonly string _backchannelsDirectory = GetBackchannelsDirectory();

    /// <summary>
    /// Gets the collection of active AppHost connections.
    /// </summary>
    public IReadOnlyDictionary<string, AppHostConnection> Connections => _connections;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Wait for the command to be selected, with a timeout
            // If timeout occurs or no command is set, monitoring is not needed
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeout.Token);
            
            var command = await executionContext.CommandSelected.Task.WaitAsync(combined.Token).ConfigureAwait(false);

            // Only monitor if the command is MCP start command
            if (command is not McpStartCommand)
            {
                logger.LogDebug("Current command is not MCP start command. Auxiliary backchannel monitoring disabled.");
                return;
            }

            logger.LogInformation("Starting auxiliary backchannel monitor for MCP start command");

            // Ensure the backchannels directory exists
            if (!Directory.Exists(_backchannelsDirectory))
            {
                Directory.CreateDirectory(_backchannelsDirectory);
            }

            // Monitor the directory for changes
            using var watcher = new FileSystemWatcher(_backchannelsDirectory)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                Filter = "aux.sock.*",
                EnableRaisingEvents = true
            };

            watcher.Created += OnSocketCreated;
            watcher.Deleted += OnSocketDeleted;

            // Scan for existing sockets on startup
            await ScanExistingSocketsAsync(stoppingToken).ConfigureAwait(false);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Auxiliary backchannel monitor stopping");
        }
        catch (OperationCanceledException)
        {
            // Timeout occurred - no command was selected, monitoring not needed
            logger.LogDebug("No command selected within timeout. Auxiliary backchannel monitoring not needed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in auxiliary backchannel monitor");
        }
        finally
        {
            // Clean up all connections
            foreach (var connection in _connections.Values)
            {
                await DisconnectAsync(connection).ConfigureAwait(false);
            }
            _connections.Clear();
        }
    }

    private void OnSocketCreated(object sender, FileSystemEventArgs e)
    {
        logger.LogDebug("Socket created: {SocketPath}", e.FullPath);
        _ = Task.Run(async () => await TryConnectToSocketAsync(e.FullPath).ConfigureAwait(false));
    }

    private void OnSocketDeleted(object sender, FileSystemEventArgs e)
    {
        logger.LogDebug("Socket deleted: {SocketPath}", e.FullPath);
        var hash = ExtractHashFromSocketPath(e.FullPath);
        if (!string.IsNullOrEmpty(hash) && _connections.TryRemove(hash, out var connection))
        {
            _ = Task.Run(async () => await DisconnectAsync(connection).ConfigureAwait(false));
        }
    }

    private async Task ScanExistingSocketsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Scanning for existing auxiliary sockets in {Directory}", _backchannelsDirectory);

        var socketFiles = Directory.GetFiles(_backchannelsDirectory, "aux.sock.*");
        logger.LogInformation("Found {Count} existing socket(s)", socketFiles.Length);

        foreach (var socketPath in socketFiles)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await TryConnectToSocketAsync(socketPath, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task TryConnectToSocketAsync(string socketPath, CancellationToken cancellationToken = default)
    {
        var hash = ExtractHashFromSocketPath(socketPath);
        if (string.IsNullOrEmpty(hash))
        {
            logger.LogWarning("Could not extract hash from socket path: {SocketPath}", socketPath);
            return;
        }

        // Check if we're already connected
        if (_connections.ContainsKey(hash))
        {
            logger.LogDebug("Already connected to AppHost with hash {Hash}", hash);
            return;
        }

        try
        {
            logger.LogInformation("Connecting to auxiliary socket: {SocketPath}", socketPath);

            // Give the socket a moment to be ready
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            // Connect to the Unix socket
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(socketPath);
            
            await socket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);

            // Create JSON-RPC connection with proper formatter
            var stream = new NetworkStream(socket, ownsSocket: true);
            var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, BackchannelJsonSerializerContext.CreateRpcMessageFormatter()));
            rpc.StartListening();

            // Get the MCP connection info
            var mcpInfo = await rpc.InvokeAsync<McpConnectionInfo>("GetMcpConnectionInfoAsync").ConfigureAwait(false);

            var connection = new AppHostConnection(hash, socketPath, rpc, mcpInfo);

            // Set up disconnect handler
            rpc.Disconnected += (sender, args) =>
            {
                logger.LogInformation("Disconnected from AppHost {Hash}: {Reason}", hash, args.Reason);
                if (_connections.TryRemove(hash, out var conn))
                {
                    _ = Task.Run(async () => await DisconnectAsync(conn).ConfigureAwait(false));
                }
            };

            if (_connections.TryAdd(hash, connection))
            {
                logger.LogInformation("Successfully connected to AppHost {Hash}. Dashboard: {DashboardUrl}", 
                    hash, mcpInfo.EndpointUrl);
            }
            else
            {
                logger.LogWarning("Failed to add connection for AppHost {Hash}", hash);
                await DisconnectAsync(connection).ConfigureAwait(false);
            }
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
        {
            logger.LogDebug("Socket not ready yet: {SocketPath}", socketPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to socket: {SocketPath}", socketPath);
        }
    }

    private static async Task DisconnectAsync(AppHostConnection connection)
    {
        try
        {
            connection.Rpc.Dispose();
        }
        catch
        {
            // Ignore disposal errors
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static string? ExtractHashFromSocketPath(string socketPath)
    {
        var fileName = Path.GetFileName(socketPath);
        if (fileName.StartsWith("aux.sock.", StringComparison.Ordinal))
        {
            return fileName["aux.sock.".Length..];
        }
        return null;
    }

    private static string GetBackchannelsDirectory()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDirectory, ".aspire", "cli", "backchannels");
    }
}

/// <summary>
/// Represents a connection to an AppHost instance via the auxiliary backchannel.
/// </summary>
internal sealed class AppHostConnection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppHostConnection"/> class.
    /// </summary>
    public AppHostConnection(string hash, string socketPath, JsonRpc rpc, McpConnectionInfo mcpInfo)
    {
        Hash = hash;
        SocketPath = socketPath;
        Rpc = rpc;
        McpInfo = mcpInfo;
        ConnectedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the hash identifier for this AppHost instance.
    /// </summary>
    public string Hash { get; }

    /// <summary>
    /// Gets the socket path for this connection.
    /// </summary>
    public string SocketPath { get; }

    /// <summary>
    /// Gets the JSON-RPC proxy for communicating with the AppHost.
    /// </summary>
    public JsonRpc Rpc { get; }

    /// <summary>
    /// Gets the MCP connection information for the Dashboard.
    /// </summary>
    public McpConnectionInfo McpInfo { get; }

    /// <summary>
    /// Gets the timestamp when this connection was established.
    /// </summary>
    public DateTimeOffset ConnectedAt { get; }
}

/// <summary>
/// Represents the connection information for the Dashboard MCP server.
/// </summary>
internal sealed class McpConnectionInfo
{
    /// <summary>
    /// Gets or sets the endpoint URL for the Dashboard MCP server.
    /// </summary>
    public required string EndpointUrl { get; init; }

    /// <summary>
    /// Gets or sets the API token for authenticating with the Dashboard MCP server.
    /// </summary>
    public required string ApiToken { get; init; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Aspire.Cli.Commands;
using Microsoft.Extensions.FileProviders;
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

    // Track known socket files to detect additions and removals
    private readonly HashSet<string> _knownSocketFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the collection of active AppHost connections.
    /// </summary>
    public IReadOnlyDictionary<string, AppHostConnection> Connections => _connections;

    /// <summary>
    /// Gets or sets the path to the selected AppHost. When set, this AppHost will be used for MCP operations.
    /// </summary>
    public string? SelectedAppHostPath { get; set; }

    /// <summary>
    /// Gets the currently selected AppHost connection based on the selection logic.
    /// </summary>
    public AppHostConnection? SelectedConnection
    {
        get
        {
            var connections = _connections.Values.ToList();

            if (connections.Count == 0)
            {
                return null;
            }

            // Check if a specific AppHost was selected
            if (!string.IsNullOrEmpty(SelectedAppHostPath))
            {
                var selectedConnection = connections.FirstOrDefault(c =>
                    c.AppHostInfo?.AppHostPath != null &&
                    string.Equals(Path.GetFullPath(c.AppHostInfo.AppHostPath), Path.GetFullPath(SelectedAppHostPath), StringComparison.OrdinalIgnoreCase));

                if (selectedConnection != null)
                {
                    return selectedConnection;
                }

                // Clear the selection since the AppHost is no longer available
                SelectedAppHostPath = null;
            }

            // Look for in-scope connections
            var inScopeConnections = connections.Where(c => c.IsInScope).ToList();

            if (inScopeConnections.Count == 1)
            {
                return inScopeConnections[0];
            }

            // Fall back to the first available connection
            return connections.FirstOrDefault();
        }
    }

    /// <summary>
    /// Gets all connections that are within the scope of the specified working directory.
    /// </summary>
    public IReadOnlyList<AppHostConnection> GetConnectionsForWorkingDirectory(DirectoryInfo workingDirectory)
    {
        return _connections.Values
            .Where(c => IsAppHostInScopeOfDirectory(c.AppHostInfo?.AppHostPath, workingDirectory.FullName))
            .ToList();
    }

    private static bool IsAppHostInScopeOfDirectory(string? appHostPath, string workingDirectory)
    {
        if (string.IsNullOrEmpty(appHostPath))
        {
            return false;
        }

        // Normalize the paths for comparison
        var normalizedWorkingDirectory = Path.GetFullPath(workingDirectory);
        var normalizedAppHostPath = Path.GetFullPath(appHostPath);

        // Check if the AppHost path is within the working directory
        var relativePath = Path.GetRelativePath(normalizedWorkingDirectory, normalizedAppHostPath);
        return !relativePath.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relativePath);
    }

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

            // Scan for existing sockets on startup
            await ProcessDirectoryChangesAsync(stoppingToken).ConfigureAwait(false);

            // Use PhysicalFileProvider with polling for cross-platform compatibility
            // FileSystemWatcher doesn't work reliably on macOS, so we use PhysicalFileProvider
            // which falls back to polling when the DOTNET_USE_POLLING_FILE_WATCHER env var is set
            // or when UsePollingFileWatcher is set to true
            using var fileProvider = new PhysicalFileProvider(_backchannelsDirectory);

            // Enable polling on macOS where FileSystemWatcher doesn't work reliably
            if (OperatingSystem.IsMacOS())
            {
                fileProvider.UsePollingFileWatcher = true;
                fileProvider.UseActivePolling = true;
            }

            // Continuously watch for changes using IAsyncEnumerable
            await foreach (var _ in WatchForChangesAsync(fileProvider, stoppingToken))
            {
                // Process the changes by rescanning the directory
                await ProcessDirectoryChangesAsync(stoppingToken).ConfigureAwait(false);
            }
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

    private async Task ProcessDirectoryChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var currentFiles = new HashSet<string>(
                Directory.GetFiles(_backchannelsDirectory, "aux.sock.*"),
                StringComparer.OrdinalIgnoreCase);

            // Find new files (files that exist now but weren't known before)
            var newFiles = currentFiles.Except(_knownSocketFiles, StringComparer.OrdinalIgnoreCase);
            foreach (var newFile in newFiles)
            {
                logger.LogDebug("Socket created: {SocketPath}", newFile);
                await TryConnectToSocketAsync(newFile, cancellationToken).ConfigureAwait(false);
            }

            // Find removed files (files that were known but no longer exist)
            var removedFiles = _knownSocketFiles.Except(currentFiles, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var removedFile in removedFiles)
            {
                logger.LogDebug("Socket deleted: {SocketPath}", removedFile);
                var hash = ExtractHashFromSocketPath(removedFile);
                if (!string.IsNullOrEmpty(hash) && _connections.TryRemove(hash, out var connection))
                {
                    await DisconnectAsync(connection).ConfigureAwait(false);
                }
            }

            // Update the known files set
            _knownSocketFiles.Clear();
            foreach (var file in currentFiles)
            {
                _knownSocketFiles.Add(file);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Error processing directory changes");
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

            // Create the auxiliary backchannel wrapper
            var backchannel = new AuxiliaryBackchannel(rpc);

            // Get the AppHost information
            var appHostInfo = await backchannel.GetAppHostInformationAsync(cancellationToken).ConfigureAwait(false);

            // Get the MCP connection info
            var mcpInfo = await backchannel.GetDashboardMcpConnectionInfoAsync(cancellationToken).ConfigureAwait(false);

            // Determine if this AppHost is in scope of the MCP server's working directory
            var isInScope = IsAppHostInScope(appHostInfo?.AppHostPath);

            var connection = new AppHostConnection(hash, socketPath, rpc, backchannel, mcpInfo, appHostInfo, isInScope);

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
                logger.LogInformation(
                    "Successfully connected to AppHost {Hash}. " +
                    "AppHost Path: {AppHostPath}, " +
                    "AppHost PID: {AppHostPid}, " +
                    "CLI PID: {CliPid}, " +
                    "Dashboard URL: {DashboardUrl}, " +
                    "Dashboard Token: {DashboardToken}, " +
                    "In Scope: {InScope}",
                    hash,
                    appHostInfo?.AppHostPath ?? "N/A",
                    appHostInfo?.ProcessId.ToString(CultureInfo.InvariantCulture) ?? "N/A",
                    appHostInfo?.CliProcessId?.ToString(CultureInfo.InvariantCulture) ?? "N/A",
                    mcpInfo?.EndpointUrl ?? "N/A",
                    mcpInfo?.ApiToken is not null ? "***" + mcpInfo.ApiToken[^4..] : "N/A",
                    isInScope);
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

    private bool IsAppHostInScope(string? appHostPath)
    {
        if (string.IsNullOrEmpty(appHostPath))
        {
            return false;
        }

        // Normalize the paths for comparison
        var workingDirectory = Path.GetFullPath(executionContext.WorkingDirectory.FullName);
        var normalizedAppHostPath = Path.GetFullPath(appHostPath);

        // Check if the AppHost path is within the working directory using a robust, cross-platform method
        var relativePath = Path.GetRelativePath(workingDirectory, normalizedAppHostPath);
        // If the relative path starts with ".." or is equal to "..", then it's outside the working directory
        return !relativePath.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relativePath);
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

    private static async IAsyncEnumerable<bool> WatchForChangesAsync(IFileProvider fileProvider, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var changeToken = fileProvider.Watch("aux.sock.*");
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            using var registration = changeToken.RegisterChangeCallback(state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), tcs);
            using var cancellationRegistration = cancellationToken.Register(() => tcs.TrySetCanceled());

            bool changed;
            try
            {
                changed = await tcs.Task.ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                yield break;
            }

            yield return changed;
        }
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
    public AppHostConnection(string hash, string socketPath, JsonRpc rpc, IAuxiliaryBackchannel backchannel, DashboardMcpConnectionInfo? mcpInfo, AppHostInformation? appHostInfo, bool isInScope)
    {
        Hash = hash;
        SocketPath = socketPath;
        Rpc = rpc;
        Backchannel = backchannel;
        McpInfo = mcpInfo;
        AppHostInfo = appHostInfo;
        IsInScope = isInScope;
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
    /// Gets the auxiliary backchannel for communicating with the AppHost.
    /// </summary>
    public IAuxiliaryBackchannel Backchannel { get; }

    /// <summary>
    /// Gets the MCP connection information for the Dashboard.
    /// </summary>
    public DashboardMcpConnectionInfo? McpInfo { get; }

    /// <summary>
    /// Gets the AppHost information.
    /// </summary>
    public AppHostInformation? AppHostInfo { get; }

    /// <summary>
    /// Gets a value indicating whether this AppHost is within the scope of the MCP server's working directory.
    /// </summary>
    public bool IsInScope { get; }

    /// <summary>
    /// Gets the timestamp when this connection was established.
    /// </summary>
    public DateTimeOffset ConnectedAt { get; }
}

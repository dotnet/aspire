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
    private readonly ConcurrentDictionary<string, AppHostAuxiliaryBackchannel> _connections = new();
    private readonly string _backchannelsDirectory = GetBackchannelsDirectory();

    // Track known socket files to detect additions and removals
    private readonly HashSet<string> _knownSocketFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _scanLock = new(1, 1);

    /// <summary>
    /// Gets the collection of active AppHost connections.
    /// </summary>
    public IReadOnlyDictionary<string, AppHostAuxiliaryBackchannel> Connections => _connections;

    /// <summary>
    /// Gets or sets the path to the selected AppHost. When set, this AppHost will be used for MCP operations.
    /// </summary>
    public string? SelectedAppHostPath { get; set; }

    /// <summary>
    /// Gets the currently selected AppHost connection based on the selection logic.
    /// </summary>
    public AppHostAuxiliaryBackchannel? SelectedConnection
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
    public IReadOnlyList<AppHostAuxiliaryBackchannel> GetConnectionsForWorkingDirectory(DirectoryInfo workingDirectory)
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

    /// <summary>
    /// Triggers an immediate scan of the backchannels directory for new/removed AppHosts.
    /// </summary>
    public Task ScanAsync(CancellationToken cancellationToken = default)
    {
        return UpdateConnectionsAsync(cancellationToken);
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
            _ = ProcessDirectoryChangesAsync(stoppingToken);

            // Use file watcher with polling enabled for reliability.
            using var fileProvider = new PhysicalFileProvider(_backchannelsDirectory);
            fileProvider.UsePollingFileWatcher = true;
            fileProvider.UseActivePolling = true;

            // Run the watcher loop until cancellation
            var fileWatcherTask = RunFileWatcherLoopAsync(fileProvider, stoppingToken);

            await fileWatcherTask.ConfigureAwait(false);
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

    private async Task UpdateConnectionsAsync(CancellationToken cancellationToken)
    {
        var connectTasks = await ProcessDirectoryChangesAsync(cancellationToken).ConfigureAwait(false);
        if (connectTasks.Count > 0)
        {
            await Task.WhenAll(connectTasks).ConfigureAwait(false);
        }
    }

    private async Task<IReadOnlyList<Task>> ProcessDirectoryChangesAsync(CancellationToken cancellationToken)
    {
        var connectTasks = new List<Task>();
        await _scanLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Support both "auxi.sock.*" (new) and "aux.sock.*" (old) for backward compatibility
            // Note: "aux" is a reserved device name on Windows < 11, but we still scan for it
            // to support connections from older CLI versions
            // Using "aux*.sock.*" wildcard to match both patterns
            var currentFiles = new HashSet<string>(
                Directory.Exists(_backchannelsDirectory)
                    ? Directory.GetFiles(_backchannelsDirectory, "aux*.sock.*")
                    : [],
                StringComparer.OrdinalIgnoreCase);

            // Find new files (files that exist now but weren't known before)
            var newFiles = currentFiles.Except(_knownSocketFiles, StringComparer.OrdinalIgnoreCase).ToList();
            connectTasks.EnsureCapacity(newFiles.Count);
            foreach (var newFile in newFiles)
            {
                logger.LogDebug("Socket created: {SocketPath}", newFile);
                connectTasks.Add(TryConnectToSocketAsync(newFile, cancellationToken));
            }

            // Find removed files (files that were known but no longer exist)
            var removedFiles = _knownSocketFiles.Except(currentFiles, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var removedFile in removedFiles)
            {
                logger.LogDebug("Socket deleted: {SocketPath}", removedFile);
                var hash = ExtractHashFromSocketPath(removedFile);
                if (!string.IsNullOrEmpty(hash) && _connections.TryRemove(hash, out var connection))
                {
                    _ = Task.Run(async () => await DisconnectAsync(connection).ConfigureAwait(false), CancellationToken.None);
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
        finally
        {
            _scanLock.Release();
        }

        return connectTasks;
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

        var maxElapsed = TimeSpan.FromSeconds(5);
        var delay = TimeSpan.FromMilliseconds(100);
        var maxDelay = TimeSpan.FromSeconds(2);
        var start = DateTimeOffset.UtcNow;
        var isFirstAttempt = true;
        Socket? socket = null;

        while (DateTimeOffset.UtcNow - start < maxElapsed)
        {
            try
            {
                if (isFirstAttempt)
                {
                    logger.LogInformation("Connecting to auxiliary socket: {SocketPath}", socketPath);
                }
                else
                {
                    logger.LogDebug("Retrying connection to auxiliary socket: {SocketPath}", socketPath);
                }

                // Give the socket a moment to be ready (exponential backoff)
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, maxDelay.TotalMilliseconds));
                isFirstAttempt = false;

                // Connect to the Unix socket
                socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                var endpoint = new UnixDomainSocketEndPoint(socketPath);

                await socket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
                break; // Success - exit retry loop
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
            {
                socket?.Dispose();
                socket = null;

                logger.LogDebug("Socket not ready yet, will retry: {SocketPath}", socketPath);
            }
            catch (Exception ex)
            {
                socket?.Dispose();
                logger.LogError(ex, "Failed to connect to socket: {SocketPath}", socketPath);
                return;
            }
        }

        if (socket is null || !socket.Connected)
        {
            logger.LogDebug("Socket connection timed out after {ElapsedSeconds} seconds: {SocketPath}", maxElapsed.TotalSeconds, socketPath);
            return;
        }

        try
        {
            // Create JSON-RPC connection with proper formatter
            var stream = new NetworkStream(socket, ownsSocket: true);
            var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, BackchannelJsonSerializerContext.CreateRpcMessageFormatter()));
            rpc.StartListening();

            // Get the AppHost information
            var appHostInfo = await rpc.InvokeAsync<AppHostInformation?>("GetAppHostInformationAsync").ConfigureAwait(false);

            // Get the MCP connection info
            var mcpInfo = await rpc.InvokeAsync<DashboardMcpConnectionInfo?>("GetDashboardMcpConnectionInfoAsync").ConfigureAwait(false);

            // Determine if this AppHost is in scope of the MCP server's working directory
            var isInScope = IsAppHostInScope(appHostInfo?.AppHostPath);

            var connection = new AppHostAuxiliaryBackchannel(hash, socketPath, rpc, mcpInfo, appHostInfo, isInScope, logger);

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

    private static async Task DisconnectAsync(AppHostAuxiliaryBackchannel connection)
    {
        try
        {
            connection.Dispose();
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
        // Support both "auxi.sock." (new) and "aux.sock." (old) for backward compatibility
        if (fileName.StartsWith("auxi.sock.", StringComparison.Ordinal))
        {
            return fileName["auxi.sock.".Length..];
        }
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

    /// <summary>
    /// Runs the file watcher loop that triggers scans when file changes are detected.
    /// </summary>
    private async Task RunFileWatcherLoopAsync(IFileProvider fileProvider, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var changed in WatchForChangesAsync(fileProvider, cancellationToken))
            {
                _ = ProcessDirectoryChangesAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown
        }
    }

    /// <summary>
    /// Watches for file changes in the backchannels directory using change tokens.
    /// </summary>
    private static async IAsyncEnumerable<bool> WatchForChangesAsync(IFileProvider fileProvider, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Watch for both "auxi.sock.*" (new) and "aux.sock.*" (old) patterns for backward compatibility
            // Using "aux*.sock.*" wildcard to match both patterns
            var changeToken = fileProvider.Watch("aux*.sock.*");
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

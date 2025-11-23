// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Aspire.Hosting.Backchannel;

/// <summary>
/// Background service that listens for multiple concurrent connections on a Unix socket
/// and provides MCP-related RPC operations via the auxiliary backchannel.
/// </summary>
/// <remarks>
/// Unlike the existing backchannel which accepts a single connection, the auxiliary backchannel
/// supports multiple concurrent client connections. Each connection gets its own RPC target instance.
/// </remarks>
internal sealed class AuxiliaryBackchannelService(
    ILogger<AuxiliaryBackchannelService> logger,
    IConfiguration configuration,
    IDistributedApplicationEventing eventing,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    private Socket? _serverSocket;

    /// <summary>
    /// Gets the Unix socket path where the auxiliary backchannel is listening.
    /// </summary>
    /// <remarks>
    /// The socket path follows the pattern: $HOME/.aspire/cli/backchannels/aux.sock.[hash]
    /// where [hash] is derived from AppHost:PathSha256 configuration value for uniqueness.
    /// </remarks>
    public string? SocketPath { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Create the socket path
            SocketPath = GetAuxiliaryBackchannelSocketPath(configuration);
            
            logger.LogDebug("Starting auxiliary backchannel service on socket path: {SocketPath}", SocketPath);

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(SocketPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Clean up any existing socket file
            if (File.Exists(SocketPath))
            {
                File.Delete(SocketPath);
            }

            // Create and bind the server socket
            _serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(SocketPath);
            _serverSocket.Bind(endpoint);
            _serverSocket.Listen(backlog: 10); // Allow multiple pending connections

            logger.LogInformation("Auxiliary backchannel listening on {SocketPath}", SocketPath);

            // Accept connections in a loop (supporting multiple concurrent connections)
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var clientSocket = await _serverSocket.AcceptAsync(stoppingToken).ConfigureAwait(false);
                    
                    // Handle each connection on a separate task
                    _ = Task.Run(async () => await HandleClientConnectionAsync(clientSocket, stoppingToken).ConfigureAwait(false), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Expected when shutting down
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error accepting client connection on auxiliary backchannel");
                }
            }
        }
        catch (TaskCanceledException ex)
        {
            logger.LogDebug("Auxiliary backchannel service was cancelled: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in auxiliary backchannel service");
        }
        finally
        {
            // Clean up the socket
            _serverSocket?.Dispose();
            if (SocketPath != null && File.Exists(SocketPath))
            {
                try
                {
                    File.Delete(SocketPath);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete socket file: {SocketPath}", SocketPath);
                }
            }
        }
    }

    private async Task HandleClientConnectionAsync(Socket clientSocket, CancellationToken stoppingToken)
    {
        try
        {
            logger.LogDebug("Client connected to auxiliary backchannel");

            // Publish the connected event
            var connectedEvent = new AuxiliaryBackchannelConnectedEvent(serviceProvider, SocketPath!);
            await eventing.PublishAsync(
                connectedEvent,
                EventDispatchBehavior.NonBlockingConcurrent,
                stoppingToken).ConfigureAwait(false);

            // Create a new RPC target for this connection
            var rpcTarget = new AuxiliaryBackchannelRpcTarget(
                serviceProvider.GetRequiredService<ILogger<AuxiliaryBackchannelRpcTarget>>(),
                serviceProvider);

            // Set up JSON-RPC over the client socket
            using var stream = new NetworkStream(clientSocket, ownsSocket: true);
            using var rpc = JsonRpc.Attach(stream, rpcTarget);
            
            // Wait for the connection to be disposed (client disconnect or cancellation)
            await rpc.Completion.ConfigureAwait(false);
            
            logger.LogDebug("Client disconnected from auxiliary backchannel");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Client connection handler was cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling client connection on auxiliary backchannel");
        }
    }

    /// <summary>
    /// Generates the Unix socket path for the auxiliary backchannel.
    /// </summary>
    /// <remarks>
    /// Pattern: $HOME/.aspire/cli/backchannels/aux.sock.[hash]
    /// The hash is derived from AppHost:PathSha256 configuration to ensure uniqueness per app host instance.
    /// Falls back to process ID hash if configuration value is not available.
    /// </remarks>
    private static string GetAuxiliaryBackchannelSocketPath(IConfiguration configuration)
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var backchannelsDir = Path.Combine(homeDirectory, ".aspire", "cli", "backchannels");
        
        // Use AppHost:PathSha256 from configuration for consistent hashing
        var appHostPathSha = configuration["AppHost:PathSha256"];
        string hash;
        
        if (!string.IsNullOrEmpty(appHostPathSha))
        {
            // Use first 16 characters to keep socket path length reasonable (Unix socket path limits)
            hash = appHostPathSha[..16].ToLowerInvariant();
        }
        else
        {
            // Fallback: Generate a hash from the current process ID for uniqueness
            var processId = Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(processId));
            hash = Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
        }
        
        var socketPath = Path.Combine(backchannelsDir, $"aux.sock.{hash}");
        return socketPath;
    }
}

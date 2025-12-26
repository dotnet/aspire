// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.RemoteHost;

/// <summary>
/// Entry point for running the RemoteHost server.
/// </summary>
public static class RemoteHostServer
{
    /// <summary>
    /// Runs the RemoteHost JSON-RPC server.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="cancellationToken">Cancellation token to stop the server.</param>
    /// <returns>A task that completes when the server has stopped.</returns>
    public static async Task RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Start the orphan detector to monitor the parent process
        var orphanDetector = new OrphanDetector();
        var orphanDetectorTask = orphanDetector.StartAsync(cts.Token);

        orphanDetector.OnParentDied = () =>
        {
            Console.WriteLine("Parent process died, shutting down...");
            cts.Cancel();
        };

        var socketPath = Environment.GetEnvironmentVariable("REMOTE_APP_HOST_SOCKET_PATH");
        if (string.IsNullOrEmpty(socketPath))
        {
            var tempDir = Path.GetTempPath();
            socketPath = Path.Combine(tempDir, "aspire", "remote-app-host.sock");
        }

        Console.WriteLine($"Starting RemoteAppHost JsonRpc Server on {socketPath}...");
        Console.WriteLine("This server will continue running until stopped with Ctrl+C");

        await using var server = new JsonRpcServer(socketPath);

        server.OnAllClientsDisconnected = () =>
        {
            Console.WriteLine("All clients disconnected, shutting down server...");
            cts.Cancel();
        };

        // Handle graceful shutdown
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\nShutting down server...");
            cts.Cancel();
        };

        // Handle SIGTERM
        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            Console.WriteLine("Process exit requested, shutting down...");
            cts.Cancel();
        };

        try
        {
            await server.StartAsync(cts.Token);
            Console.WriteLine("Server has stopped gracefully.");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Server shutdown requested.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server error: {ex.Message}");
            throw;
        }
        finally
        {
            // Stop the orphan detector
            await orphanDetector.StopAsync(CancellationToken.None);
        }

        Console.WriteLine("Goodbye!");
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.RemoteHost;

/// <summary>
/// Entry point for running the RemoteHost server.
/// </summary>
public static class RemoteHostServer
{
    /// <summary>
    /// Runs the RemoteHost JSON-RPC server, loading ATS assemblies from appsettings.json.
    /// </summary>
    /// <remarks>
    /// The server reads the "AtsAssemblies" section from appsettings.json to determine which
    /// assemblies to scan for [AspireExport] capabilities. The appsettings.json should be
    /// in the current working directory.
    /// </remarks>
    /// <param name="args">Command line arguments.</param>
    /// <param name="cancellationToken">Cancellation token to stop the server.</param>
    /// <returns>A task that completes when the server has stopped.</returns>
    public static Task RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var assemblyNames = configuration.GetSection("AtsAssemblies").Get<string[]>() ?? [];
        var assemblies = LoadAssemblies(assemblyNames);

        return RunAsync(args, assemblies, cancellationToken);
    }

    private static IEnumerable<Assembly> LoadAssemblies(string[] assemblyNames)
    {
        var loaded = new List<Assembly>();

        foreach (var name in assemblyNames)
        {
            try
            {
                var assembly = Assembly.Load(name);
                loaded.Add(assembly);
                Console.WriteLine($"[ATS] Loaded assembly: {name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ATS] Warning: Failed to load assembly '{name}': {ex.Message}");
            }
        }

        return loaded;
    }

    /// <summary>
    /// Runs the RemoteHost JSON-RPC server.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="atsAssemblies">The assemblies to scan for ATS capabilities and handles.</param>
    /// <param name="cancellationToken">Cancellation token to stop the server.</param>
    /// <returns>A task that completes when the server has stopped.</returns>
    public static async Task RunAsync(string[] args, IEnumerable<Assembly> atsAssemblies, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Start the orphan detector to monitor the parent process
        var orphanDetector = new OrphanDetector();
        _ = orphanDetector.StartAsync(cts.Token);

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

        // Read auth token from environment variable (set by CLI)
        var authToken = Environment.GetEnvironmentVariable("ASPIRE_RPC_AUTH_TOKEN");

        // Check for hot reload mode
        var enableHotReload = args.Contains("--hot-reload");

        Console.WriteLine($"Starting RemoteAppHost JsonRpc Server on {socketPath}...");
        Console.WriteLine(authToken != null ? "Authentication is enabled." : "Authentication is disabled.");
        Console.WriteLine(enableHotReload ? "Hot reload is enabled - server will wait for client reconnections." : "Hot reload is disabled.");
        Console.WriteLine("This server will continue running until stopped with Ctrl+C");

        var server = new JsonRpcServer(socketPath, atsAssemblies, authToken);
        try
        {
            // In hot reload mode, don't shutdown when clients disconnect
            // The server stays running so new clients can reconnect
            if (!enableHotReload)
            {
                server.OnAllClientsDisconnected = () =>
                {
                    Console.WriteLine("All clients disconnected, shutting down server...");
                    cts.Cancel();
                };
            }

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
                try
                {
                    cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // CTS already disposed, server already shutting down
                }
            };

            try
            {
                await server.StartAsync(cts.Token).ConfigureAwait(false);
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
        }
        finally
        {
            await server.DisposeAsync().ConfigureAwait(false);
            // Stop the orphan detector
            await orphanDetector.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }

        Console.WriteLine("Goodbye!");
    }
}

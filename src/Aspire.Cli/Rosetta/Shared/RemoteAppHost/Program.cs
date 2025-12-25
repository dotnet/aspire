// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RemoteAppHost;

var cts = new CancellationTokenSource();

// Start the orphan detector to monitor the parent process
var orphanDetector = new OrphanDetector();
var orphanDetectorTask = orphanDetector.StartAsync(cts.Token);

// When orphan detector detects parent death, it calls Environment.Exit(0)
// But we also want to signal the server to stop
orphanDetector.OnParentDied = () =>
{
    Console.WriteLine("Parent process died, shutting down...");
    cts.Cancel();
};

var socketPath = Environment.GetEnvironmentVariable("REMOTE_APP_HOST_SOCKET_PATH");
if (string.IsNullOrEmpty(socketPath))
{
    // Default socket path
    var tempDir = Path.GetTempPath();
    socketPath = Path.Combine(tempDir, "aspire", "remote-app-host.sock");
}

Console.WriteLine($"Starting RemoteAppHost JsonRpc Server on {socketPath}...");
Console.WriteLine("This server will continue running until stopped with Ctrl+C");

using var server = new JsonRpcServer(socketPath);

// When the last client disconnects, shut down the server
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
    Environment.Exit(1);
}

// Stop the orphan detector
await orphanDetector.StopAsync(CancellationToken.None);

Console.WriteLine("Goodbye!");

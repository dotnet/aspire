// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RemoteAppHost;

_ = new OrphanDetector().StartAsync(default);

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

// Handle graceful shutdown
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nShutting down server...");
    server.Stop();
};

try
{
    await server.StartAsync();
    Console.WriteLine("Server has stopped gracefully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Server error: {ex.Message}");
    Environment.Exit(1);
}

Console.WriteLine("Goodbye!");

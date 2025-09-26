// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RemoteAppHost;

Console.WriteLine("🚀 Starting RemoteAppHost JsonRpc Server...");
Console.WriteLine("This server will continue running until stopped with Ctrl+C");

var server = new JsonRpcServer(Environment.GetEnvironmentVariable("REMOTE_APP_HOST_PIPE_NAME") ?? "RemoteAppHost");

// Handle graceful shutdown
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\n🛑 Shutting down server...");
    server.Stop();
};

try
{
    await server.StartAsync();
    Console.WriteLine("✅ Server has stopped gracefully.");
}
catch (Exception ex)
{
    server.Stop();
    Console.WriteLine($"❌ Server error: {ex.Message}");
    Environment.Exit(1);
}

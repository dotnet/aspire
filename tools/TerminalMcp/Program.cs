// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using TerminalMcp;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (MCP uses stdout for communication)
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Register the terminal session manager as a singleton
builder.Services.AddSingleton<TerminalSessionManager>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync().ConfigureAwait(false);

/// <summary>
/// Simple ping tool to verify the MCP server is working.
/// </summary>
[McpServerToolType]
public static class PingTool
{
    /// <summary>
    /// Returns a pong response to verify the server is working.
    /// </summary>
    /// <returns>A pong message with the current timestamp.</returns>
    [McpServerTool, Description("Verify the TerminalMcp server is running and responsive.")]
    public static string Ping()
    {
        return $"pong! TerminalMcp server is running. Server time: {DateTime.UtcNow:O}";
    }
}

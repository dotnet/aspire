// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync().ConfigureAwait(false);

[McpServerToolType]
public static class AspireProcessTools
{
    [McpServerTool, Description("Kills instances of the 'aspire' CLI process.")]
    public static string KillAspireCliProcesses()
    {
        var processes = System.Diagnostics.Process.GetProcessesByName("aspire");
        var killedProcessIds = new List<int>();

        foreach (var process in processes)
        {
            try
            {
                process.Kill();
                killedProcessIds.Add(process.Id);
            }
            catch (Exception ex)
            {
                return $"Failed to kill process {process.Id}: {ex.Message}";
            }
        }

        return killedProcessIds.Count switch
        {
            0 => "No 'aspire' processes found.",
            1 => $"Killed 1 'aspire' process (PID: {killedProcessIds[0]}).",
            _ => $"Killed {killedProcessIds.Count} 'aspire' processes (PIDs: {string.Join(", ", killedProcessIds)})."
        };
    }
}
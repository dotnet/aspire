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
internal static class AspireProcessTools
{
    [McpServerTool, Description("Kills all instances of the 'aspire' CLI process.")]
    public static string KillAllAspireCliProcesses()
    {
        var processes = System.Diagnostics.Process.GetProcessesByName("aspire");
        var killedProcessIds = new List<int>();
        var failedProcessKills = new List<(int ProcessId, string ErrorMessage)>();

        foreach (var process in processes)
        {
            try
            {
                process.Kill();
                killedProcessIds.Add(process.Id);
            }
            catch (Exception ex)
            {
                // This will probably never happen, but we catch it
                // and report just in case.
                failedProcessKills.Add((process.Id, ex.Message));
            }
        }

        var resultMessage = killedProcessIds.Count switch
        {
            0 => "No 'aspire' processes found.",
            1 => $"Killed 1 'aspire' process (PID: {killedProcessIds[0]}).",
            _ => $"Killed {killedProcessIds.Count} 'aspire' processes (PIDs: {string.Join(", ", killedProcessIds)})."
        };

        if (failedProcessKills.Count > 0)
        {
            var failedMessage = $"Failed to kill {failedProcessKills.Count} process(es): " +
                                $"{string.Join("; ", failedProcessKills.Select(fp => $"PID {fp.ProcessId}: {fp.ErrorMessage}"))}.";
            resultMessage += $" {failedMessage}";
        }

        return resultMessage;
    }
}
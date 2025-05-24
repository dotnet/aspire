// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Aspire.Cli.Commands;

internal sealed class McpCommand : BaseCommand
{
    public McpCommand() : base("mcp", "Start an MCP server")
    {
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Logging.AddConsole(consoleLogOptions =>
        {
            // Configure all logs to go to stderr
            consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        builder.Services.AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<Greeter>();

        await builder.Build().RunAsync(cancellationToken);
        return 0;
    }
}

[McpServerToolType]
public class Greeter(IHostApplicationLifetime lifetime)
{
    [McpServerTool, Description("Say hello from .NET Aspire")]
    public string SayHello(string name)
    {
        _ = lifetime;
        return $"Hello {name} from .NET Aspire!";
    }
}
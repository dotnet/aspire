// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Utils;
using ModelContextProtocol.Protocol;

namespace Aspire.Dashboard.Mcp;

public static class McpExtensions
{
    public static IMcpServerBuilder AddAspireMcpTools(this IServiceCollection services)
    {
        var builder = services.AddMcpServer(options =>
        {
            options.ServerInfo = new Implementation
            {
                Name = "Aspire MCP",
                Version = VersionHelpers.DashboardDisplayVersion ?? "1.0.0"
            };
            options.ServerInstructions =
                """
                ## Description
                This MCP Server provides various tools for managing Aspire resources, logs, traces and commands.

                ## Instructions
                - When a resource name is returned, render it in bold chars like **resourceName**
                - When a resource state (running, stopped, starting, ...) is returned, render it in italic chars like *running*, and add a colored badge next to it (green, red, orange, ...).

                ## Tools

                """;
        }).WithHttpTransport();

        builder.WithTools<AspireMcpTools>();

        return builder;
    }
}

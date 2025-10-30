// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Utils;
using ModelContextProtocol.Protocol;

namespace Aspire.Dashboard.Mcp;

public static class McpExtensions
{
    public static IMcpServerBuilder AddAspireMcpTools(this IServiceCollection services, DashboardOptions dashboardOptions)
    {
        var builder = services.AddMcpServer(options =>
        {
            // SVG isn't a required icon format for MCP. Use PNGs to ensure the icon is visible in all tools that support icons.
            var sizes = new string[] { "16", "32", "48", "64", "256" };
            var icons = sizes.Select(s =>
            {
                using var stream = typeof(McpExtensions).Assembly.GetManifestResourceStream($"Aspire.Dashboard.Mcp.Resources.aspire-{s}.png")!;

                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                var data = memoryStream.ToArray();

                return new Icon { Source = $"data:image/png;base64,{Convert.ToBase64String(data)}", MimeType = "image/png", Sizes = [s] };
            }).ToList();

            options.ServerInfo = new Implementation
            {
                Name = "Aspire MCP",
                Version = VersionHelpers.DashboardDisplayVersion ?? "1.0.0",
                Icons = icons
            };
            options.ServerInstructions =
                """
                ## Description
                This MCP Server provides various tools for managing Aspire resources, logs, traces and commands.

                ## Instructions
                - When a resource, structured log or trace is returned, include a link to the Aspire dashboard using dashboard_link
                - When a resource state (running, stopped, starting, ...) is returned, and add an emoji colored badge next to it (green, red, orange, etc).

                ## Tools

                """;
        }).WithHttpTransport();

        // Always register telemetry tools
        builder.WithTools<AspireTelemetryMcpTools>();

        // Only register resource tools if the resource service is configured
        if (dashboardOptions.ResourceServiceClient.GetUri() is not null)
        {
            builder.WithTools<AspireResourceMcpTools>();
        }

        return builder;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp.Prompts;

/// <summary>
/// MCP prompt that provides an Aspire pair programmer persona.
/// </summary>
internal sealed class AspirePairProgrammerPrompt : CliMcpPrompt
{
    public override string Name => KnownMcpPrompts.AspirePairProgrammer;

    public override string Description => "Activates an Aspire pair programmer persona with deep knowledge of Aspire concepts, best practices, and documentation.";

    public override IReadOnlyList<PromptArgument>? GetArguments() =>
    [
        new PromptArgument
        {
            Name = "context",
            Description = "Optional context about what you're working on (e.g., 'adding Redis caching', 'deploying to Azure').",
            Required = false
        }
    ];

    public override GetPromptResult GetPrompt(IReadOnlyDictionary<string, string>? arguments)
    {
        var context = arguments?.GetValueOrDefault("context") ?? "";
        var contextClause = string.IsNullOrEmpty(context)
            ? ""
            : $"\n\nThe user is currently working on: {context}";

        var systemPrompt = $"""
            You are an expert Aspire pair programmer with deep knowledge of Aspire, distributed applications, and cloud-native development.

            ## Your Expertise

            - **Aspire architecture**: AppHost, ServiceDefaults, orchestration, and the resource model
            - **Integrations**: Redis, PostgreSQL, SQL Server, MongoDB, RabbitMQ, Kafka, Azure services, and 40+ other integrations
            - **Deployment**: Azure Container Apps, Kubernetes, Docker Compose, and custom deployment pipelines
            - **Observability**: OpenTelemetry, structured logging, distributed tracing, and the Aspire Dashboard
            - **Best practices**: Service discovery, health checks, configuration, and security

            ## Available Tools

            You have access to Aspire MCP tools. Use them to:

            ### Documentation Tools
            - **list_docs**: Browse all available aspire.dev documentation pages with titles and summaries
            - **search_docs**: Search documentation using keywords (e.g., 'redis connection string', 'health checks')
            - **get_doc**: Retrieve full content of a specific documentation page by slug

            ### Resource & Observability Tools
            - **list_resources**: Check the status of running resources
            - **list_console_logs**: View console output from resources
            - **list_structured_logs**: Analyze structured log entries
            - **list_traces**: Investigate distributed traces
            - **execute_resource_command**: Execute commands on resources (start, stop, restart)

            ### Integration & Environment Tools
            - **list_integrations**: Discover available Aspire integrations
            - **doctor**: Diagnose Aspire environment issues

            ## Guidelines

            1. **Use the Aspire CLI** for operations like running, publishing, and deploying apps. Not the .NET CLI directly.
            2. **Search documentation** with `search_docs` when answering questions about Aspire features or APIs.
            3. **Get full docs** with `get_doc` when you need detailed information on a specific topic.
            4. **Check resource status** before troubleshooting issues.
            5. **Provide code examples** that follow Aspire conventions and patterns.
            6. **Suggest integrations** from the available gallery when appropriate.
            {contextClause}
            """;

        var result = new GetPromptResult
        {
            Messages =
            [
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock { Text = systemPrompt }
                }
            ]
        };

        return result;
    }
}

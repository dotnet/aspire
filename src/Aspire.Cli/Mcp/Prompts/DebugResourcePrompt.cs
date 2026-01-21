// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp.Prompts;

/// <summary>
/// MCP prompt for debugging resources in an Aspire application.
/// </summary>
internal sealed class DebugResourcePrompt : CliMcpPrompt
{
    public override string Name => KnownMcpPrompts.DebugResource;

    public override string Description => "Helps debug issues with a specific resource in your Aspire application.";

    public override IReadOnlyList<PromptArgument>? GetArguments() =>
    [
        new PromptArgument
        {
            Name = "resourceName",
            Description = "The name of the resource to debug (e.g., 'apiservice', 'redis', 'postgres').",
            Required = true
        },
        new PromptArgument
        {
            Name = "issue",
            Description = "A description of the issue you're experiencing.",
            Required = false
        }
    ];

    public override GetPromptResult GetPrompt(IReadOnlyDictionary<string, string>? arguments)
    {

        var resourceName = arguments?.GetValueOrDefault("resourceName") ?? "unknown";
        var issue = arguments?.GetValueOrDefault("issue") ?? "unspecified issue";

        var prompt = $"""
            I need help debugging the '{resourceName}' resource in my Aspire application.

            **Issue:** {issue}

            Please help me investigate by:

            1. **Check resource status** - Use `list_resources` to see the current state and health of '{resourceName}'
            2. **Review console logs** - Use `list_console_logs` with resourceName='{resourceName}' to check for errors or warnings
            3. **Analyze structured logs** - Use `list_structured_logs` filtered to '{resourceName}' for detailed log entries
            4. **Check traces** - Use `list_traces` to see if there are any failed requests or slow operations
            5. **Suggest fixes** - Based on the findings, recommend solutions

            If the resource isn't running or has errors, suggest how to fix it using Aspire CLI commands or configuration changes.
            """;

        var result = new GetPromptResult
        {
            Messages =
            [
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock { Text = prompt }
                }
            ]
        };

        return result;
    }
}

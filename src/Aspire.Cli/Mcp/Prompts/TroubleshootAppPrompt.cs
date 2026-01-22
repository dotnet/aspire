// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp.Prompts;

/// <summary>
/// MCP prompt for troubleshooting an Aspire application.
/// </summary>
internal sealed class TroubleshootAppPrompt : CliMcpPrompt
{
    public override string Name => KnownMcpPrompts.TroubleshootApp;

    public override string Description => "Comprehensive troubleshooting for your Aspire application, analyzing resources, logs, traces, and providing recommendations.";

    public override IReadOnlyList<PromptArgument>? GetArguments() =>
    [
        new PromptArgument
        {
            Name = "symptom",
            Description = "Describe the issue or symptom you're experiencing (e.g., 'app won't start', 'slow responses', 'connection errors').",
            Required = true
        }
    ];

    public override GetPromptResult GetPrompt(IReadOnlyDictionary<string, string>? arguments)
    {

        var symptom = arguments?.GetValueOrDefault("symptom") ?? "application issues";

        var prompt = $"""
            I'm experiencing issues with my Aspire application: {symptom}

            Please perform a comprehensive troubleshooting analysis:

            ## Step 1: Environment Check
            - Use `doctor` to verify the Aspire environment is properly configured

            ## Step 2: Resource Status
            - Use `list_resources` to check the status and health of all resources
            - Identify any resources that are not running, unhealthy, or have errors

            ## Step 3: Log Analysis
            - For any problematic resources, use `list_console_logs` to check for startup errors
            - Use `list_structured_logs` to find error and warning level entries

            ## Step 4: Trace Analysis
            - Use `list_traces` to identify failed or slow operations
            - Look for patterns in the traces that might indicate the root cause

            ## Step 5: Documentation Lookup
            - Use `search_docs` to find relevant troubleshooting documentation for the symptom
            - Use `get_doc` to retrieve detailed documentation for specific topics

            ## Step 6: Recommendations
            Based on the analysis, provide:
            1. **Root cause** - What's likely causing the issue
            2. **Immediate fix** - Commands or changes to resolve the issue now
            3. **Long-term solution** - Best practices to prevent this issue in the future
            4. **Related resources** - Links or references to relevant documentation
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

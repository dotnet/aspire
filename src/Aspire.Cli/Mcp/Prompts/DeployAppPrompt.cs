// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp.Prompts;

/// <summary>
/// MCP prompt for deploying an Aspire application.
/// </summary>
internal sealed class DeployAppPrompt : CliMcpPrompt
{
    public override string Name => KnownMcpPrompts.DeployApp;

    public override string Description => "Guides you through deploying your Aspire application to a target environment (Azure, Kubernetes, Docker Compose, etc.).";

    public override IReadOnlyList<PromptArgument>? GetArguments() =>
    [
        new PromptArgument
        {
            Name = "target",
            Description = "The deployment target (e.g., 'azure', 'kubernetes', 'docker-compose').",
            Required = true
        },
        new PromptArgument
        {
            Name = "environment",
            Description = "The target environment name (e.g., 'staging', 'production').",
            Required = false
        }
    ];

    public override GetPromptResult GetPrompt(IReadOnlyDictionary<string, string>? arguments)
    {

        var target = arguments?.GetValueOrDefault("target") ?? "azure";
        var environment = arguments?.GetValueOrDefault("environment") ?? "production";

        var prompt = $"""
            I want to deploy my Aspire application to {target} for the {environment} environment.

            Please help me by:

            1. **Check environment** - Use `doctor` to verify my Aspire environment is properly configured
            2. **Fetch deployment docs** - Use `search_aspire_docs` to find deployment documentation for {target}

            Then guide me through:

            1. **Prerequisites** - What do I need to have set up before deploying to {target}?
            2. **Publishing** - Show me the `aspire publish` command to generate deployment artifacts
            3. **Deployment** - Show me the `aspire deploy` command or equivalent for {target}
            4. **Configuration** - How to set environment-specific configuration and secrets
            5. **Verification** - How to verify the deployment was successful

            Use Aspire CLI commands (`aspire publish`, `aspire deploy`) rather than direct cloud CLI commands where possible.

            For {target} specifically, what are the recommended practices and any gotchas I should be aware of?
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

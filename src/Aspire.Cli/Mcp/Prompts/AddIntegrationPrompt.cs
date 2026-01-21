// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp.Prompts;

/// <summary>
/// MCP prompt for adding integrations to an Aspire application.
/// </summary>
internal sealed class AddIntegrationPrompt : CliMcpPrompt
{
    public override string Name => KnownMcpPrompts.AddIntegration;

    public override string Description => "Guides you through adding a new integration (database, cache, messaging, etc.) to your Aspire application.";

    public override IReadOnlyList<PromptArgument>? GetArguments() =>
    [
        new PromptArgument
        {
            Name = "integrationType",
            Description = "The type of integration you want to add (e.g., 'redis', 'postgresql', 'rabbitmq', 'azure-storage'). If you know the NuGet package name, you can provide that as well.",
            Required = true
        },
        new PromptArgument
        {
            Name = "resourceName",
            Description = "The name to give the resource in your AppHost (e.g., 'cache', 'db', 'messaging'). One is inferred from the integration type in a meaningful way if not provided.",
            Required = false
        }
    ];

    public override GetPromptResult GetPrompt(IReadOnlyDictionary<string, string>? arguments)
    {

        var integrationType = arguments?.GetValueOrDefault("integrationType") ?? "unknown";
        var resourceName = arguments?.GetValueOrDefault("resourceName") ?? integrationType.ToLowerInvariant();

        var prompt = $"""
            I want to add a {integrationType} integration to my Aspire application with resource name '{resourceName}'.

            Please help me by:

            1. **Find the right package** - Use `list_integrations` and search for '{integrationType}' to find the appropriate Aspire hosting and client packages
            2. **Get documentation** - Use `get_integration_docs` to get the official documentation for the integration
            3. **Fetch Aspire docs** - Use `fetch_aspire_docs` or `search_aspire_docs` for detailed setup instructions

            Then provide:

            1. **AppHost configuration** - Show me the code to add to my AppHost (Program.cs) to configure the resource
            2. **Client configuration** - Show me how to configure the client integration in consuming projects
            3. **Connection usage** - Show me how to use the connection in my application code
            4. **Best practices** - Any tips for using {integrationType} with Aspire effectively

            Use the Aspire CLI commands for adding packages (e.g., `aspire add`) rather than `dotnet add`.
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

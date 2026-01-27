// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Mcp.Skills;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp.Tools;

/// <summary>
/// MCP tool for saving a skill to disk.
/// Allows AI agents to persist in-memory skills to the user's skills directory.
/// </summary>
internal sealed class SaveSkillTool(ISkillsProvider skillsProvider) : CliMcpTool
{
    private readonly ISkillsProvider _skillsProvider = skillsProvider;

    public override string Name => KnownMcpTools.SaveSkill;

    public override string Description => """
        Saves a skill to disk in the user's skills directory.
        Creates a new skill folder with a SKILL.md file containing the skill content.
        The skill will be available as an MCP resource after saving.
        Use this to persist AI-generated or user-created skills for future use.
        """;

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse($$"""
            {
              "type": "object",
              "properties": {
                "skillName": {
                  "type": "string",
                  "description": "The name of the skill (used as the folder name). Should be lowercase with hyphens, e.g., 'my-custom-skill'."
                },
                "content": {
                  "type": "string",
                  "description": "The skill content in Markdown format. Should include instructions, best practices, or guidance for the AI agent."
                },
                "description": {
                  "type": "string",
                  "description": "Optional brief description of the skill (used in YAML frontmatter for discovery)."
                },
                "targetDirectory": {
                  "type": "string",
                  "description": "Optional target directory path. Defaults to ~/.aspire/skills/ if not specified."
                }
              },
              "required": ["skillName", "content"],
              "additionalProperties": false,
              "description": "Saves a skill to disk. Requires skillName and content. Optionally accepts description and targetDirectory."
            }
            """).RootElement;
    }

    public override async ValueTask<CallToolResult> CallToolAsync(
        ModelContextProtocol.Client.McpClient mcpClient,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        CancellationToken cancellationToken)
    {
        _ = mcpClient;

        if (arguments is null)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "Arguments are required. Please provide 'skillName' and 'content'." }]
            };
        }

        if (!arguments.TryGetValue("skillName", out var skillNameElement) || skillNameElement.ValueKind != JsonValueKind.String)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'skillName' parameter is required and must be a string." }]
            };
        }

        if (!arguments.TryGetValue("content", out var contentElement) || contentElement.ValueKind != JsonValueKind.String)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'content' parameter is required and must be a string." }]
            };
        }

        var skillName = skillNameElement.GetString()!;
        var content = contentElement.GetString()!;

        string? description = null;
        if (arguments.TryGetValue("description", out var descElement) && descElement.ValueKind == JsonValueKind.String)
        {
            description = descElement.GetString();
        }

        string? targetDirectory = null;
        if (arguments.TryGetValue("targetDirectory", out var dirElement) && dirElement.ValueKind == JsonValueKind.String)
        {
            targetDirectory = dirElement.GetString();
        }

        try
        {
            var savedPath = await _skillsProvider.SaveSkillAsync(
                skillName,
                content,
                description,
                targetDirectory,
                cancellationToken).ConfigureAwait(false);

            return new CallToolResult
            {
                Content = [new TextContentBlock
                {
                    Text = $"""
                        # Skill Saved Successfully

                        **Skill Name:** {skillName}
                        **Saved To:** {savedPath}

                        The skill is now available as an MCP resource at `skill://{skillName}`.
                        """
                }]
            };
        }
        catch (ArgumentException ex)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = $"Invalid argument: {ex.Message}" }]
            };
        }
        catch (IOException ex)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = $"Failed to save skill: {ex.Message}" }]
            };
        }
    }
}

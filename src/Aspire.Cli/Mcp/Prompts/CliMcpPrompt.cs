// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp.Prompts;

/// <summary>
/// Base class for MCP prompts in the Aspire CLI.
/// </summary>
internal abstract class CliMcpPrompt
{
    /// <summary>
    /// Gets the name of the prompt.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the description of the prompt.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Gets the list of arguments for this prompt.
    /// </summary>
    /// <returns>The list of prompt arguments, or null if no arguments are required.</returns>
    public virtual IReadOnlyList<PromptArgument>? GetArguments() => null;

    /// <summary>
    /// Gets the prompt content.
    /// </summary>
    /// <param name="arguments">The arguments passed to the prompt.</param>
    /// <returns>The prompt result containing messages.</returns>
    public abstract GetPromptResult GetPrompt(IReadOnlyDictionary<string, string>? arguments);

    /// <summary>
    /// Converts this prompt to an MCP Prompt descriptor.
    /// </summary>
    public Prompt ToPrompt() => new()
    {
        Name = Name,
        Description = Description,
        Arguments = GetArguments()?.ToList()
    };
}

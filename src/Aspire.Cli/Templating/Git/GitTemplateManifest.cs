// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Cli.Templating.Git;

/// <summary>
/// Represents the deserialized content of an <c>aspire-template.json</c> file.
/// </summary>
internal sealed class GitTemplateManifest
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    public int Version { get; set; } = 1;

    public required string Name { get; set; }

    public LocalizableString? DisplayName { get; set; }

    public LocalizableString? Description { get; set; }

    public string? Language { get; set; }

    public List<string>? Scope { get; set; }

    public Dictionary<string, GitTemplateVariable>? Variables { get; set; }

    public GitTemplateSubstitutions? Substitutions { get; set; }

    public Dictionary<string, string>? ConditionalFiles { get; set; }

    public List<string>? PostMessages { get; set; }

    public List<GitTemplatePostInstruction>? PostInstructions { get; set; }
}

/// <summary>
/// A post-application instruction block shown to the user after template creation.
/// </summary>
internal sealed class GitTemplatePostInstruction
{
    /// <summary>
    /// Gets or sets the heading displayed above the instruction lines (rendered as a slug line).
    /// </summary>
    public required LocalizableString Heading { get; set; }

    /// <summary>
    /// Gets or sets whether this is a primary or secondary instruction group.
    /// Primary instructions are visually highlighted. Defaults to <c>"secondary"</c>.
    /// </summary>
    public string Priority { get; set; } = "secondary";

    /// <summary>
    /// Gets or sets the instruction lines to display.
    /// Lines may contain <c>{{variable}}</c> placeholders that are substituted with variable values.
    /// </summary>
    public required List<string> Lines { get; set; }

    /// <summary>
    /// Gets or sets an optional condition expression that controls whether this instruction is shown.
    /// Format: <c>"variableName == value"</c> or <c>"variableName != value"</c>.
    /// When omitted, the instruction is always shown.
    /// </summary>
    public string? Condition { get; set; }
}

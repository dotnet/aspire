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

    public string? Description { get; set; }

    public string? Language { get; set; }

    public Dictionary<string, GitTemplateVariable>? Variables { get; set; }

    public GitTemplateSubstitutions? Substitutions { get; set; }

    public Dictionary<string, string>? ConditionalFiles { get; set; }

    public List<string>? PostMessages { get; set; }
}

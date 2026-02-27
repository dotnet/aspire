// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Cli.Templating.Git;

/// <summary>
/// Represents the deserialized content of an <c>aspire-template-index.json</c> file.
/// </summary>
internal sealed class GitTemplateIndex
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    public int Version { get; set; } = 1;

    public GitTemplateIndexPublisher? Publisher { get; set; }

    public List<GitTemplateIndexEntry> Templates { get; set; } = [];

    public List<GitTemplateIndexInclude>? Includes { get; set; }
}

/// <summary>
/// Publisher information for a template index.
/// </summary>
internal sealed class GitTemplateIndexPublisher
{
    public required string Name { get; set; }

    public string? Url { get; set; }

    public bool? Verified { get; set; }
}

/// <summary>
/// An entry in a template index pointing to a template.
/// </summary>
internal sealed class GitTemplateIndexEntry
{
    public required string Name { get; set; }

    public required string DisplayName { get; set; }

    public required string Description { get; set; }

    public required string Path { get; set; }

    public string? Repo { get; set; }

    public string? Language { get; set; }

    public List<string>? Tags { get; set; }

    public string? MinAspireVersion { get; set; }
}

/// <summary>
/// A reference to another template index (federation).
/// </summary>
internal sealed class GitTemplateIndexInclude
{
    public required string Url { get; set; }

    public string? Description { get; set; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;

namespace Aspire.Dashboard.Model.Markdown;

public sealed class MarkdownOptions
{
    public required MarkdownPipeline Pipeline { get; init; }
    public required bool SuppressSurroundingParagraph { get; init; }
    public required HashSet<string>? AllowedUrlSchemes { get; init; }
    public bool IncompleteDocument { get; internal set; }
}

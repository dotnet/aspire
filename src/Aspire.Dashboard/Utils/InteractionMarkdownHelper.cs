// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Utils;

public static class InteractionMarkdownHelper
{
    private static readonly MarkdownPipeline s_markdownPipeline = MarkdownHelpers.CreateMarkdownPipelineBuilder().Build();

    public static MarkupString ToMarkupString(string markdown)
    {
        return (MarkupString)ToHtml(markdown);
    }

    public static string ToHtml(string markdown)
    {
        // Avoid adding paragraphs to HTML output from Markdown content unless there are multiple lines (aka multiple paragraphs).
        var hasNewline = markdown.Contains('\n') || markdown.Contains('\r');

        return MarkdownHelpers.ToHtml(markdown, s_markdownPipeline, !hasNewline);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Utils;

public static class InteractionMarkdownHelper
{
    private static readonly MarkdownPipeline s_markdownPipeline = MarkdownHelpers.CreateMarkdownPipelineBuilder().Build();

    public static MarkupString ToMarkupString(string markdown, bool suppressSurroundingParagraph = false)
    {
        return (MarkupString)ToHtml(markdown, suppressSurroundingParagraph);
    }

    public static string ToHtml(string markdown, bool suppressSurroundingParagraph = false)
    {
        return MarkdownHelpers.ToHtml(markdown, s_markdownPipeline, suppressSurroundingParagraph);
    }
}

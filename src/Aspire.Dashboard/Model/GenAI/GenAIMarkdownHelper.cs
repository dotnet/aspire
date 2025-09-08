// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Utils;
using Markdig;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Model.GenAI;

public static class GenAIMarkdownHelper
{
    private static readonly MarkdownPipeline s_markdownPipeline = MarkdownHelpers.CreateMarkdownPipelineBuilder().Build();

    public static MarkupString ToMarkupString(string markdown)
    {
        return (MarkupString)ToHtml(markdown);
    }

    public static string ToHtml(string markdown)
    {
        // GenAI responses are untrusted, so only allow safe schemes.
        var options = new MarkdownOptions
        {
            Pipeline = s_markdownPipeline,
            AllowedUrlSchemes = MarkdownHelpers.SafeUrlSchemes,
            SuppressSurroundingParagraph = false
        };

        return MarkdownHelpers.ToHtml(markdown, options);
    }
}

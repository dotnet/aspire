// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Resources;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Microsoft.Extensions.Localization;

namespace Aspire.Dashboard.Model.Markdown;

public class AspireCodeBlockExtension : IMarkdownExtension
{
    private readonly IStringLocalizer<ControlsStrings> _loc;

    public AspireCodeBlockExtension(IStringLocalizer<ControlsStrings> loc)
    {
        _loc = loc;
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer)
        {
            // Must remove the built-in renderer so the new one runs.
            var originalCodeBlockRenderer = htmlRenderer.ObjectRenderers.FindExact<CodeBlockRenderer>();
            if (originalCodeBlockRenderer != null)
            {
                htmlRenderer.ObjectRenderers.Remove(originalCodeBlockRenderer);
            }

            htmlRenderer.ObjectRenderers.Add(new HighlightedCodeBlockRenderer(_loc));
        }
    }
}

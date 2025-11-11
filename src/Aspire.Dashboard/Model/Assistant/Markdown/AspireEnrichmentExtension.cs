// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Markdig.Renderers;

namespace Aspire.Dashboard.Model.Assistant.Markdown;

public class AspireEnrichmentExtension : IMarkdownExtension
{
    private readonly AspireEnrichmentOptions _options;

    public AspireEnrichmentExtension(AspireEnrichmentOptions options)
    {
        _options = options;
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.InlineParsers.Contains<AspireEnrichmentParser>())
        {
            // Insert the parser before any other parsers
            pipeline.InlineParsers.Insert(0, new AspireEnrichmentParser(_options));
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer)
        {
            htmlRenderer.ObjectRenderers.Add(new ResourceInlineRenderer());
            htmlRenderer.ObjectRenderers.Add(new LogEntryInlineRenderer());
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Markdig.Extensions.AutoLinks;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Aspire.Dashboard.Utils;

public static class MarkdownHelpers
{
    private static readonly HashSet<string> s_allowedSchemes = new HashSet<string>(StringComparers.EndpointAnnotationUriScheme) { "http", "https", "mailto" };

    public static MarkdownPipelineBuilder CreateMarkdownPipelineBuilder()
    {
        var autoLinkOptions = new AutoLinkOptions
        {
            OpenInNewWindow = true,
            AllowDomainWithoutPeriod = true
        };
        autoLinkOptions.ValidPreviousCharacters += "`";

        var pipelineBuilder = new MarkdownPipelineBuilder();
        pipelineBuilder.ConfigureNewLine(Environment.NewLine);
        pipelineBuilder.DisableHtml();
        pipelineBuilder.UseAutoLinks(autoLinkOptions);
        pipelineBuilder.UseGridTables();
        pipelineBuilder.UsePipeTables();
        pipelineBuilder.UseEmphasisExtras();

        return pipelineBuilder;
    }

    public static string ToHtml(string markdown, MarkdownPipeline pipeline, bool suppressSurroundingParagraph = false)
    {
        // markdig won't render a surrounding paragraph if HtmlRenderer.ImplicitParagraph is true.
        // The naming is odd, but I think the idea is we're telling the renderer that there is an implicit paragraph
        // around the content and so renderer doesn't need to add one.
        return ToHtml(markdown, pipeline, suppressSurroundingParagraph ? render => render.ImplicitParagraph = true : null);
    }

    private static string ToHtml(string markdown, MarkdownPipeline pipeline, Action<HtmlRenderer>? setupAction)
    {
        var document = Markdown.Parse(markdown, pipeline);

        // Open absolute links in the response in a new window.
        foreach (var link in document.Descendants<LinkInline>())
        {
            switch (DetectLink(link.Url))
            {
                case LinkType.Absolute:
                    AddLinkAttributes(link);
                    break;
                case LinkType.Prohibited:
                    link.Url = string.Empty;
                    break;
            }
        }
        foreach (var link in document.Descendants<AutolinkInline>())
        {
            switch (DetectLink(link.Url))
            {
                case LinkType.Absolute:
                    AddLinkAttributes(link);
                    break;
                case LinkType.Prohibited:
                    link.Url = string.Empty;
                    break;
            }
        }

        var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        setupAction?.Invoke(renderer);
        pipeline.Setup(renderer);

        renderer.Render(document); // using the renderer directly
        writer.Flush();

        return writer.ToString();

        static LinkType DetectLink(string? url)
        {
            if (url == null || !Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
            {
                return LinkType.None;
            }

            if (!uri.IsAbsoluteUri)
            {
                return LinkType.Relative;
            }

            if (!s_allowedSchemes.Contains(uri.Scheme))
            {
                return LinkType.Prohibited;
            }

            return LinkType.Absolute;
        }

        static void AddLinkAttributes(IMarkdownObject link)
        {
            var attributes = link.GetAttributes();

            attributes.AddPropertyIfNotExist("target", "_blank");
            attributes.AddPropertyIfNotExist("rel", "noopener noreferrer nofollow");
        }
    }

    private enum LinkType
    {
        None,
        Relative,
        Absolute,
        Prohibited
    }
}

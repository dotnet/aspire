// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Aspire.Dashboard.Model.Markdown;

public static class MarkdownHelpers
{
    // These URL schemes are considered always safe to be included in Markdown received from untrusted 3rd parties, e.g. GenAI.
    // Untrusted 3rd party Markdown should be limited to these URLs to avoid links that could trigger other programs that listen for the scheme.
    public static readonly HashSet<string> SafeUrlSchemes = new HashSet<string>(StringComparers.EndpointAnnotationUriScheme) { "http", "https", "mailto" };

    public static string ToHtml(string markdown, MarkdownOptions options)
    {
        var document = Markdig.Markdown.Parse(markdown, options.Pipeline);

        // Open absolute links in the response in a new window.
        foreach (var link in document.Descendants<LinkInline>())
        {
            switch (DetectLink(link.Url, options.AllowedUrlSchemes))
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
            switch (DetectLink(link.Url, options.AllowedUrlSchemes))
            {
                case LinkType.Absolute:
                    AddLinkAttributes(link);
                    break;
                case LinkType.Prohibited:
                    link.Url = string.Empty;
                    break;
            }
        }

        // Adjust heading levels so that they are appropriate for embedding in a page.
        // Markdown can easily contain h2/h3 headings, which would be greater than the dialog title header.
        // This change ensures all headings are h4 or smaller.
        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            heading.Level = heading.Level switch
            {
                1 => 4,
                2 => 4,
                3 => 5,
                4 => 5,
                5 => 6,
                _ => 6
            };
        }

        var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        if (options.SuppressSurroundingParagraph)
        {
            // markdig won't render a surrounding paragraph if HtmlRenderer.ImplicitParagraph is true.
            // The naming is odd, but I think the idea is we're telling the renderer that there is an implicit paragraph
            // around the content and so renderer doesn't need to add one.
            renderer.ImplicitParagraph = true;
        }

        options.Pipeline.Setup(renderer);

        renderer.Render(document); // using the renderer directly
        writer.Flush();

        return writer.ToString();

        static LinkType DetectLink(string? url, HashSet<string>? allowedUrlSchemes)
        {
            if (url == null || !Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
            {
                return LinkType.None;
            }

            if (!uri.IsAbsoluteUri)
            {
                return LinkType.Relative;
            }

            if (allowedUrlSchemes != null)
            {
                if (!allowedUrlSchemes.Contains(uri.Scheme))
                {
                    return LinkType.Prohibited;
                }
            }
            else
            {
                // Even if no allowed schemes are specified, always block "javascript:" links.
                if (string.Equals(uri.Scheme, "javascript", StringComparison.OrdinalIgnoreCase))
                {
                    return LinkType.Prohibited;
                }
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

    public static bool GetSuppressSurroundingParagraph(string markdown, bool suppressParagraphOnNewLines)
    {
        // Avoid adding paragraphs to HTML output from Markdown content unless there are multiple lines (aka multiple paragraphs).
        return suppressParagraphOnNewLines && !(markdown.Contains('\n') || markdown.Contains('\r'));
    }

    private enum LinkType
    {
        None,
        Relative,
        Absolute,
        Prohibited
    }
}

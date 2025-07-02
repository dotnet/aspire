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

    public static string ToHtml(string markdown, MarkdownPipeline pipeline, bool inCompleteDocument = false, bool suppressSurroundingParagraph = false)
    {
        // markdig won't render a surrounding paragraph if HtmlRenderer.ImplicitParagraph is true.
        // The naming is odd, but I think the idea is we're telling the renderer that there is an implicit paragraph
        // around the content and so renderer doesn't need to add one.
        return ToHtml(markdown, pipeline, inCompleteDocument, suppressSurroundingParagraph ? render => render.ImplicitParagraph = true : null);
    }

    private static string ToHtml(string markdown, MarkdownPipeline pipeline, bool inCompleteDocument, Action<HtmlRenderer>? setupAction)
    {
        var document = Markdown.Parse(markdown, pipeline);

        if (inCompleteDocument)
        {
            // Don't render partially complete inline code blocks because they can be transformed when rendered to HTML.
            CompletePartialElements(document, renderPartialInlineCode: false);
        }

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

    public static void CompletePartialElements(MarkdownDocument document, bool renderPartialInlineCode)
    {
        var lastChild = document.LastChild;
        while (lastChild is ContainerBlock containerBlock)
        {
            lastChild = containerBlock.LastChild;
        }

        if (lastChild is not LeafBlock leafBlock)
        {
            return;
        }

        // "Level: 2" means '-' was used.
        if (leafBlock is HeadingBlock { IsSetext: true, Level: 2, HeaderCharCount: 1 } setext)
        {
            var paragraph = new ParagraphBlock();
            paragraph.Inline = new ContainerInline();
            setext.Inline?.EmbraceChildrenBy(paragraph.Inline);

            var parent = setext.Parent!;
            parent[parent.IndexOf(setext)] = paragraph;

            leafBlock = paragraph;
        }

        if (leafBlock.Inline?.LastChild is LiteralInline literal)
        {
            // A LiteralInline with a backtick character is a potential CodeInline that wasn't closed.
            var indexOfBacktick = literal.Content.IndexOf('`');
            if (indexOfBacktick >= 0)
            {
                // But it could also happen if the backticks were escaped.
                if (literal.Content.AsSpan().Count('`') == 1 && !literal.IsFirstCharacterEscaped)
                {
                    // "Text with `a code inline" => "Text with `a code inline`"
                    var originalLength = literal.Content.Length;

                    // Shorten the existing text. -1 to exclude the backtick.
                    literal.Content.End = indexOfBacktick - 1;

                    // Insert a CodeInline with the remainder. +1 and -1 to account for the backtick.
                    if (renderPartialInlineCode)
                    {
                        var code = literal.Content.Text.Substring(indexOfBacktick + 1, originalLength - literal.Content.Length - 1);
                        literal.InsertAfter(new CodeInline(code));
                    }

                    return;
                }
            }

            var previousSibling = literal.PreviousSibling;

            // Handle unclosed bold/italic that don't yet have any following content.
            if (previousSibling is null && IsEmphasisStart(literal.Content.AsSpan()))
            {
                literal.Remove();
                return;
            }

            if (previousSibling is EmphasisInline)
            {
                // Handle cases like "**_foo_ and bar" by skipping the _foo_ emphasis.
                previousSibling = previousSibling.PreviousSibling;
            }

            if (previousSibling is LiteralInline previousInline)
            {
                var content = previousInline.Content.AsSpan();

                // Unclosed bold/italic (EmphasisInline)?
                // Note that this doesn't catch cases with mixed opening chars, e.g. "**_text"
                if (IsEmphasisStart(content))
                {
                    literal.Remove();

                    var emphasis = new EmphasisInline();
                    emphasis.DelimiterChar = '*';
                    emphasis.DelimiterCount = previousInline.Content.Length;

                    previousInline.ReplaceBy(emphasis);

                    if (emphasis.DelimiterCount <= 2)
                    {
                        // Just * or **
                        emphasis.AppendChild(literal);
                    }
                    else
                    {
                        // E.g. "***text", which we need to turn into nested <em><strong>text</strong></em>
                        emphasis.DelimiterCount = 1;

                        var nestedStrong = new EmphasisInline();
                        nestedStrong.DelimiterChar = emphasis.DelimiterChar;
                        nestedStrong.DelimiterCount = 2;

                        nestedStrong.AppendChild(literal);
                        emphasis.AppendChild(nestedStrong);
                    }

                    if (emphasis.NextSibling is EmphasisInline nextSibling)
                    {
                        // This is the EmphasisInline we've skipped before. Fix the ordering.
                        // "**_foo_ and bar" is currently "** and bar**_foo_".
                        // Move the skipped emphasis to be the first child of the node we've generated.
                        nextSibling.Remove();
                        emphasis.FirstChild!.InsertBefore(nextSibling);
                    }

                    return;
                }
                else if (content is "[" or "![")
                {
                    // In-progress link, e.g. [text](http://
                    literal.Remove();
                    previousInline.Remove();
                }
            }
        }
        else if (leafBlock.Inline?.LastChild is LinkDelimiterInline linkDelimiterInline)
        {
            // In-progress link, e.g. [text, or [text]
            linkDelimiterInline.Remove();
        }
        else if (leafBlock.Inline?.LastChild is LinkInline linkInline)
        {
            // In-progress link, e.g. [text](http://
            if (!linkInline.IsClosed)
            {
                linkInline.Remove();
            }
        }

        static bool IsEmphasisStart(ReadOnlySpan<char> text)
        {
            return text is "*" or "**" or "***" or "_" or "__" or "___";
        }
    }
}

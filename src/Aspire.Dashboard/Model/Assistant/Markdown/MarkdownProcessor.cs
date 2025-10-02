// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Resources;
using Markdig;
using Markdig.Extensions.AutoLinks;
using Markdig.Parsers;
using Microsoft.Extensions.Localization;

namespace Aspire.Dashboard.Model.Assistant.Markdown;

public sealed class MarkdownProcessor
{
    private readonly MarkdownPipeline _markdownPipeline;

    public MarkdownProcessor(AssistantChatDataContext dataContext, IStringLocalizer<ControlsStrings> loc)
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
        // Remove IndentedCodeBlockParser because VS messes up indenting around code blocks.
        // This should be fine because code blocks returned by model should always be fenced.
        // Consider removing once VS is fixed.
        pipelineBuilder.BlockParsers.RemoveAll(p => p is IndentedCodeBlockParser);
        pipelineBuilder.Extensions.Add(new AspireEnrichmentExtension(new AspireEnrichmentOptions
        {
            DataContext = dataContext,
            Loc = loc
        }));
        _markdownPipeline = pipelineBuilder.Build();
    }

    public string ToHtml(string markdown, bool inCompleteDocument = false, bool suppressSurroundingParagraph = false)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        return MarkdownHelpers.ToHtml(markdown, _markdownPipeline, inCompleteDocument, suppressSurroundingParagraph);
    }
}

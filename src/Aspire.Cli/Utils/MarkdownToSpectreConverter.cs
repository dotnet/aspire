// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Aspire.Cli.Utils;

/// <summary>
/// Converts basic Markdown syntax to Spectre.Console markup for CLI display.
/// </summary>
internal static partial class MarkdownToSpectreConverter
{
    /// <summary>
    /// Converts markdown text to Spectre.Console markup.
    /// Supports basic markdown elements: headers, bold, italic, links, and inline code.
    /// </summary>
    /// <param name="markdown">The markdown text to convert.</param>
    /// <returns>The converted Spectre.Console markup text.</returns>
    public static string ConvertToSpectre(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return markdown;
        }

        var result = markdown;

        // Normalize line endings to LF to ensure consistent output
        result = result.Replace("\r\n", "\n").Replace("\r", "\n");

        // Process quoted text (> text) - do this first as it's line-based
        result = ConvertQuotedText(result);

        // Process multi-line code blocks (```) - do this before inline code
        result = ConvertCodeBlocks(result);

        // Process headers (# ## ### #### ##### ######)
        result = ConvertHeaders(result);

        // Process bold text (**bold** or __bold__)
        result = ConvertBold(result);

        // Process italic text (*italic* or _italic_)
        result = ConvertItalic(result);

        // Process strikethrough text (~~text~~)
        result = ConvertStrikethrough(result);

        // Process inline code (`code`)
        result = ConvertInlineCode(result);

        // Process images ![alt](url) - remove them as they can't be displayed in CLI
        result = ConvertImages(result);

        // Process links [text](url)
        result = ConvertLinks(result);

        // Escape any remaining square brackets that could be interpreted as Spectre markup
        result = EscapeRemainingSquareBrackets(result);

        return result;
    }

    private static string ConvertHeaders(string text)
    {
        // Convert ###### Header 6 (most specific first)
        text = HeaderLevel6Regex().Replace(text, "[bold]$1[/]");
        
        // Convert ##### Header 5
        text = HeaderLevel5Regex().Replace(text, "[bold]$1[/]");
        
        // Convert #### Header 4
        text = HeaderLevel4Regex().Replace(text, "[bold]$1[/]");
        
        // Convert ### Header 3
        text = HeaderLevel3Regex().Replace(text, "[bold yellow]$1[/]");
        
        // Convert ## Header 2
        text = HeaderLevel2Regex().Replace(text, "[bold blue]$1[/]");
        
        // Convert # Header 1
        text = HeaderLevel1Regex().Replace(text, "[bold green]$1[/]");

        return text;
    }

    private static string ConvertBold(string text)
    {
        // Convert **bold** and __bold__
        text = BoldDoubleAsterisksRegex().Replace(text, "[bold]$1[/]");
        text = BoldDoubleUnderscoresRegex().Replace(text, "[bold]$1[/]");
        
        return text;
    }

    private static string ConvertItalic(string text)
    {
        // Convert *italic* and _italic_ (but not ** or __)
        text = ItalicSingleAsteriskRegex().Replace(text, "[italic]$1[/]");
        text = ItalicSingleUnderscoreRegex().Replace(text, "[italic]$1[/]");
        
        return text;
    }

    private static string ConvertStrikethrough(string text)
    {
        // Convert ~~strikethrough~~
        return StrikethroughRegex().Replace(text, "[strikethrough]$1[/]");
    }

    private static string ConvertCodeBlocks(string text)
    {
        // Convert multi-line code blocks ```code```
        return CodeBlockRegex().Replace(text, "[grey]$1[/]");
    }

    private static string ConvertQuotedText(string text)
    {
        // Convert > quoted text
        return QuotedTextRegex().Replace(text, "[italic grey]$1[/]");
    }

    private static string ConvertInlineCode(string text)
    {
        // Convert `code`
        return InlineCodeRegex().Replace(text, "[grey][bold]$1[/][/]");
    }

    private static string ConvertImages(string text)
    {
        // Remove image references ![alt](url) as they can't be displayed in CLI
        return ImageRegex().Replace(text, "");
    }

    private static string ConvertLinks(string text)
    {
        // Convert [text](url) to just the URL with underline and blue color
        return LinkRegex().Replace(text, "[blue underline]$2[/]");
    }

    private static string EscapeRemainingSquareBrackets(string text)
    {
        // Escape any remaining square brackets that are not part of Spectre markup
        // We need to preserve Spectre markup tags like [bold], [/], [blue underline], etc.
        // but escape markdown constructs like reference links [text][ref]
        
        // Use a regex to find standalone square brackets that are not Spectre markup
        // Spectre markup pattern: [word] or [word word] or [/] 
        // Reference/other markdown pattern: everything else with square brackets
        
        // First, temporarily replace all Spectre markup with placeholders
        var spectreMarkups = new List<string>();
        var spectrePattern = @"\[(?:/?(?:bold|italic|grey|blue|green|yellow|underline|strikethrough)\s?)+\]|\[/\]";
        var spectreRegex = new Regex(spectrePattern);
        
        var textWithPlaceholders = spectreRegex.Replace(text, match =>
        {
            var placeholder = $"__SPECTRE_MARKUP_{spectreMarkups.Count}__";
            spectreMarkups.Add(match.Value);
            return placeholder;
        });
        
        // Now escape remaining square brackets
        textWithPlaceholders = textWithPlaceholders.Replace("[", "[[").Replace("]", "]]");
        
        // Restore Spectre markup
        for (int i = 0; i < spectreMarkups.Count; i++)
        {
            textWithPlaceholders = textWithPlaceholders.Replace($"__SPECTRE_MARKUP_{i}__", spectreMarkups[i]);
        }
        
        return textWithPlaceholders;
    }

    [GeneratedRegex(@"^###### (.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex HeaderLevel6Regex();

    [GeneratedRegex(@"^##### (.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex HeaderLevel5Regex();

    [GeneratedRegex(@"^#### (.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex HeaderLevel4Regex();

    [GeneratedRegex(@"^### (.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex HeaderLevel3Regex();

    [GeneratedRegex(@"^## (.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex HeaderLevel2Regex();

    [GeneratedRegex(@"^# (.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex HeaderLevel1Regex();

    [GeneratedRegex(@"\*\*([^*]+)\*\*")]
    private static partial Regex BoldDoubleAsterisksRegex();

    [GeneratedRegex(@"__([^_]+)__")]
    private static partial Regex BoldDoubleUnderscoresRegex();

    [GeneratedRegex(@"(?<!\*)\*([^*\n]+)\*(?!\*)")]
    private static partial Regex ItalicSingleAsteriskRegex();

    [GeneratedRegex(@"(?<!_)_([^_\n]+)_(?!_)")]
    private static partial Regex ItalicSingleUnderscoreRegex();

    [GeneratedRegex(@"~~([^~]+)~~")]
    private static partial Regex StrikethroughRegex();

    [GeneratedRegex(@"```\s*(.*?)\s*```", RegexOptions.Singleline)]
    private static partial Regex CodeBlockRegex();

    [GeneratedRegex(@"^> (.+?)$", RegexOptions.Multiline)]
    private static partial Regex QuotedTextRegex();

    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"!\[([^\]]*)\]\(([^)]+)\)")]
    private static partial Regex ImageRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)")]
    private static partial Regex LinkRegex();
}
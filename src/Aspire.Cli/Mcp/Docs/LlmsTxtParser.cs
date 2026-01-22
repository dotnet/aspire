// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Aspire.Cli.Mcp.Docs;

/// <summary>
/// Represents a parsed document from llms.txt format.
/// </summary>
internal sealed class LlmsDocument
{
    /// <summary>
    /// Gets the document title (from H1).
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the document slug (URL-friendly title).
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Gets the document summary (from blockquote).
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// Gets the full document content (including title and summary).
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the document sections (H2 and below).
    /// </summary>
    public required IReadOnlyList<LlmsSection> Sections { get; init; }
}

/// <summary>
/// Represents a section within a document.
/// </summary>
internal sealed class LlmsSection
{
    /// <summary>
    /// Gets the section heading text.
    /// </summary>
    public required string Heading { get; init; }

    /// <summary>
    /// Gets the heading level (2 for H2, 3 for H3, etc.).
    /// </summary>
    public required int Level { get; init; }

    /// <summary>
    /// Gets the section content (from heading to next heading of same or higher level).
    /// </summary>
    public required string Content { get; init; }
}

/// <summary>
/// Parser for llms.txt format documentation with parallel document processing.
/// </summary>
internal static partial class LlmsTxtParser
{
    private const int EstimatedDocumentsPerFile = 250;
    private const int EstimatedSectionsPerDocument = 15;

    /// <summary>
    /// Parses llms.txt content into a collection of documents using parallel processing.
    /// </summary>
    /// <param name="content">The raw llms.txt content.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a list of parsed documents.</returns>
    public static async Task<IReadOnlyList<LlmsDocument>> ParseAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        // Find all document boundaries (line indices where H1 headers start)
        var docBoundaries = FindDocumentBoundaries(content);
        if (docBoundaries.Count is 0)
        {
            return [];
        }

        // Parse documents in parallel using Task.WhenAll
        var parseTasks = new Task<LlmsDocument?>[docBoundaries.Count];

        for (var i = 0; i < docBoundaries.Count; i++)
        {
            var startIndex = docBoundaries[i];
            var endIndex = i + 1 < docBoundaries.Count
                ? docBoundaries[i + 1]
                : content.Length;

            var docContent = content.AsMemory(startIndex, endIndex - startIndex);
            parseTasks[i] = Task.Run(() => ParseDocument(docContent.Span), cancellationToken);
        }

        var documents = await Task.WhenAll(parseTasks).ConfigureAwait(false);

        // Filter out nulls and return
        return documents.Where(static d => d is not null).ToList()!;
    }

    /// <summary>
    /// Finds the character indices where each H1 header starts.
    /// </summary>
    private static List<int> FindDocumentBoundaries(string content)
    {
        var boundaries = new List<int>(EstimatedDocumentsPerFile);
        var span = content.AsSpan();
        var position = 0;

        // Check if content starts with H1
        if (IsH1Start(span))
        {
            boundaries.Add(0);
        }

        // Find all newline + H1 patterns
        while (position < span.Length)
        {
            var newlineIndex = span[position..].IndexOf('\n');
            if (newlineIndex < 0)
            {
                break;
            }

            position += newlineIndex + 1;

            if (position < span.Length && IsH1Start(span[position..]))
            {
                boundaries.Add(position);
            }
        }

        return boundaries;
    }

    /// <summary>
    /// Checks if the span starts with an H1 header.
    /// </summary>
    private static bool IsH1Start(ReadOnlySpan<char> span)
    {
        // Skip leading whitespace
        var trimmed = span.TrimStart();

        // Must start with "# " (single # followed by space)
        if (trimmed.Length < 2)
        {
            return false;
        }

        return trimmed[0] is '#'
            && trimmed[1] is not '#'
            && (trimmed[1] is ' ' || trimmed.Length is 1);
    }

    /// <summary>
    /// Parses a single document from a content span.
    /// </summary>
    private static LlmsDocument? ParseDocument(ReadOnlySpan<char> docSpan)
    {
        if (docSpan.IsEmpty)
        {
            return null;
        }

        // Find the first line (H1 title)
        var firstNewline = docSpan.IndexOf('\n');
        var titleLine = firstNewline >= 0 ? docSpan[..firstNewline] : docSpan;

        // Extract title text (remove leading #)
        var title = ExtractHeadingText(titleLine);
        if (title.Length is 0)
        {
            return null;
        }

        var titleString = title.ToString();

        // Find summary (first blockquote after title)
        var remaining = firstNewline >= 0 ? docSpan[(firstNewline + 1)..] : [];
        var summary = FindSummary(remaining);

        // Parse sections
        var sections = ParseSections(docSpan);

        // Content is the full span as string
        var content = docSpan.ToString();

        return new LlmsDocument
        {
            Title = titleString,
            Slug = GenerateSlug(titleString),
            Summary = summary,
            Content = content,
            Sections = sections
        };
    }

    /// <summary>
    /// Extracts the heading text (removes leading # characters and whitespace).
    /// </summary>
    private static ReadOnlySpan<char> ExtractHeadingText(ReadOnlySpan<char> line)
    {
        var trimmed = line.TrimStart();

        // Skip # characters
        var hashCount = 0;
        while (hashCount < trimmed.Length && trimmed[hashCount] is '#')
        {
            hashCount++;
        }

        if (hashCount is 0)
        {
            return [];
        }

        // Skip space after #s
        var textStart = hashCount;
        if (textStart < trimmed.Length && trimmed[textStart] is ' ')
        {
            textStart++;
        }

        return trimmed[textStart..].Trim();
    }

    /// <summary>
    /// Finds the first blockquote summary in the content.
    /// </summary>
    private static string? FindSummary(ReadOnlySpan<char> content)
    {
        var position = 0;

        while (position < content.Length)
        {
            // Find start of line (skip whitespace)
            var lineStart = position;
            while (lineStart < content.Length && content[lineStart] is ' ' or '\t')
            {
                lineStart++;
            }

            // Check for blockquote
            if (lineStart < content.Length && content[lineStart] is '>')
            {
                // Find end of line
                var lineEnd = content[lineStart..].IndexOf('\n');
                var quoteLine = lineEnd >= 0
                    ? content[lineStart..(lineStart + lineEnd)]
                    : content[lineStart..];

                // Extract text after >
                var quoteText = quoteLine[1..].Trim();
                if (quoteText.Length > 0)
                {
                    return quoteText.ToString();
                }
            }

            // Move to next line
            var nextNewline = content[position..].IndexOf('\n');
            if (nextNewline < 0)
            {
                break;
            }

            position += nextNewline + 1;

            // Stop if we hit a heading (sections start)
            if (position < content.Length && content[position] is '#')
            {
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// Parses H2+ sections from a document span.
    /// </summary>
    private static List<LlmsSection> ParseSections(ReadOnlySpan<char> docSpan)
    {
        var sections = new List<LlmsSection>(EstimatedSectionsPerDocument);
        var sectionStarts = new List<(int Index, int Level, string Heading)>(EstimatedSectionsPerDocument);

        // Find all section headings (H2+) - skip first line (H1)
        var position = docSpan.IndexOf('\n');
        if (position < 0)
        {
            return sections;
        }

        position++; // Move past first newline

        while (position < docSpan.Length)
        {
            var lineStart = position;

            // Find end of line
            var remaining = docSpan[position..];
            var newlineIndex = remaining.IndexOf('\n');
            var lineEnd = newlineIndex >= 0 ? position + newlineIndex : docSpan.Length;
            var line = docSpan[lineStart..lineEnd];

            var level = GetHeadingLevel(line);
            if (level >= 2)
            {
                var heading = ExtractHeadingText(line).ToString();
                sectionStarts.Add((lineStart, level, heading));
            }

            if (newlineIndex < 0)
            {
                break;
            }

            position = lineEnd + 1;
        }

        // Build sections with content
        for (var i = 0; i < sectionStarts.Count; i++)
        {
            var (startIndex, level, heading) = sectionStarts[i];

            // Find end of this section
            var endIndex = docSpan.Length;
            for (var j = i + 1; j < sectionStarts.Count; j++)
            {
                if (sectionStarts[j].Level <= level)
                {
                    endIndex = sectionStarts[j].Index;
                    break;
                }
            }

            var sectionContent = docSpan[startIndex..endIndex].ToString();

            sections.Add(new LlmsSection
            {
                Heading = heading,
                Level = level,
                Content = sectionContent
            });
        }

        return sections;
    }

    /// <summary>
    /// Gets the heading level from a line (0 if not a heading).
    /// </summary>
    private static int GetHeadingLevel(ReadOnlySpan<char> line)
    {
        var trimmed = line.TrimStart();

        if (trimmed.IsEmpty || trimmed[0] != '#')
        {
            return 0;
        }

        var level = 0;
        while (level < trimmed.Length && trimmed[level] is '#')
        {
            level++;
        }

        // Ensure it's a valid heading (has space after #s)
        if (level > 0 && level < trimmed.Length && trimmed[level] is ' ')
        {
            return level;
        }

        return 0;
    }

    /// <summary>
    /// Generates a URL-friendly slug from a title.
    /// </summary>
    private static string GenerateSlug(string title)
    {
        // Fast path for simple titles
        var span = title.AsSpan();
        var needsProcessing = false;

        foreach (var c in span)
        {
            if (!char.IsLetterOrDigit(c) && c is not ' ' and not '-')
            {
                needsProcessing = true;
                break;
            }

            if (char.IsUpper(c))
            {
                needsProcessing = true;
                break;
            }
        }

        if (!needsProcessing && !span.Contains(' '))
        {
            return title;
        }

        // Use pooled array for building slug
        var buffer = ArrayPool<char>.Shared.Rent(title.Length);

        try
        {
            var writeIndex = 0;
            var lastWasHyphen = true; // Start true to avoid leading hyphens

            foreach (var c in span)
            {
                if (char.IsLetterOrDigit(c))
                {
                    buffer[writeIndex++] = char.ToLowerInvariant(c);
                    lastWasHyphen = false;
                }
                else if ((c is ' ' || c is '-') && !lastWasHyphen)
                {
                    buffer[writeIndex++] = '-';
                    lastWasHyphen = true;
                }
            }

            // Trim trailing hyphen
            if (writeIndex > 0 && buffer[writeIndex - 1] is '-')
            {
                --writeIndex;
            }

            return new string(buffer, 0, writeIndex);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }
}

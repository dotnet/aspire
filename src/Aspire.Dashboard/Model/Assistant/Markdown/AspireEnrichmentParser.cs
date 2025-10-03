// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Utils;
using Markdig.Extensions.AutoLinks;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax.Inlines;

namespace Aspire.Dashboard.Model.Assistant.Markdown;

public class AspireEnrichmentParser : InlineParser
{
    // Sorted from longest to shortest so longer matches are matched first.
    private static readonly string[] s_markers = { "***", "**", "~~", "*", "_" };

    private readonly AspireEnrichmentOptions _options;

    public AspireEnrichmentParser(AspireEnrichmentOptions options)
    {
        _options = options;
        OpeningCharacters = ['`'];
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var autoLinkParser = processor.Parsers.FindExact<AutoLinkParser>();

        if (slice.Length > 1)
        {
            var textStart = slice.Start + 1;
            var endIndex = slice.Text.IndexOf('`', textStart);

            if (endIndex != -1)
            {
                var contentLength = endIndex - textStart;
                var newStart = slice.Start + contentLength + 2; // add 2 for the opening and closing backticks
                var skippedEmphasisMarkersLength = 0;

                // Remove emphasis markers. These can be nested, e.g. `*_test_*` so run in a loop.
                while (MatchEmphasisMarkers(slice.Text.AsSpan(textStart, contentLength), out var match))
                {
                    textStart += match.Length;
                    contentLength -= match.Length * 2;
                    skippedEmphasisMarkersLength += match.Length * 2;
                }

                var text = slice.Text.Substring(textStart, contentLength);

                var resources = _options.DataContext.GetResources();
                if (AIHelpers.TryGetResource(resources, text, out var resource))
                {
                    var resourceName = ResourceViewModel.GetResourceName(resource, resources);

                    var linkInline = new ResourceInline
                    {
                        ResourceName = resourceName,
                        Resource = resource,
                        IsClosed = true
                    };

                    processor.Inline = linkInline;
                    slice.Start = newStart;
                    return true;
                }

                if (TryMatch("trace_id:", text, out var traceId) || TryMatch("traceId:", text, out traceId))
                {
                    if (TryInsertTraceLink(processor, traceId))
                    {
                        slice.Start = newStart;
                        return true;
                    }
                }

                if (TryMatch("span_id:", text, out var spanId) || TryMatch("spanId:", text, out spanId))
                {
                    if (TryInsertSpanLink(processor, spanId))
                    {
                        slice.Start = newStart;
                        return true;
                    }
                }

                if (TryMatch("log_id:", text, out var logId) || TryMatch("logId:", text, out logId))
                {
                    if (TryInsertLogEntryLink(processor, logId))
                    {
                        slice.Start = newStart;
                        return true;
                    }
                }

                if (TryInsertTraceLink(processor, text))
                {
                    slice.Start = newStart;
                    return true;
                }

                if (TryInsertSpanLink(processor, text))
                {
                    slice.Start = newStart;
                    return true;
                }

                if (autoLinkParser != null)
                {
                    // Check if all of the code content is a valid URL.
                    // If it is then output content clickable URL rather than code.
                    var textSlice = new StringSlice(slice.Text, textStart, textStart + contentLength - 1);
                    if (autoLinkParser.Match(processor, ref textSlice))
                    {
                        if (textSlice.Length == 0)
                        {
                            slice.Start = newStart;
                            return true;
                        }

                        processor.Inline = null;
                    }
                }
            }
        }

        return false;

        static bool TryMatch(string prefix, string text, [NotNullWhen(true)] out string? match)
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                match = text.Substring(prefix.Length).Trim();
                return true;
            }

            match = null;
            return false;
        }
    }

    private static bool MatchEmphasisMarkers(ReadOnlySpan<char> input, [NotNullWhen(true)] out string? match)
    {
        foreach (var marker in s_markers)
        {
            if (input.Length >= (marker.Length * 2) && input.StartsWith(marker) && input.EndsWith(marker))
            {
                match = marker;
                return true;
            }
        }

        match = null;
        return false;
    }

    private bool TryInsertTraceLink(InlineProcessor processor, string text)
    {
        if (_options.DataContext.TryGetTrace(text, out var trace))
        {
            var linkInline = new LinkInline
            {
                Url = DashboardUrls.TraceDetailUrl(trace.TraceId),
                IsClosed = true
            };
            linkInline.AppendChild(new CodeInline(OtlpHelpers.ToShortenedId(trace.TraceId)));

            processor.Inline = linkInline;
            return true;
        }

        return false;
    }

    private bool TryInsertLogEntryLink(InlineProcessor processor, string text)
    {
        if (!long.TryParse(text, CultureInfo.InvariantCulture, out var result))
        {
            return false;
        }

        if (_options.DataContext.TryGetLog(result, out var logEntry))
        {
            var linkInline = new LogEntryInline
            {
                LogEntry = logEntry,
                IsClosed = true
            };

            processor.Inline = linkInline;
            return true;
        }

        return false;
    }

    private bool TryInsertSpanLink(InlineProcessor processor, string text)
    {
        foreach (var trace in _options.DataContext.GetReferencedTraces())
        {
            var span = trace.Spans.FirstOrDefault(s => OtlpHelpers.MatchTelemetryId(text, s.SpanId));
            if (span != null)
            {
                var linkInline = new LinkInline
                {
                    Url = DashboardUrls.TraceDetailUrl(trace.TraceId, span.SpanId),
                    IsClosed = true
                };
                linkInline.AppendChild(new CodeInline(OtlpHelpers.ToShortenedId(span.SpanId)));

                processor.Inline = linkInline;
                return true;
            }
        }

        return false;
    }
}

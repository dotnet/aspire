// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Aspire.Shared.ConsoleLogs;

/// <summary>
/// Shared AI helper methods for console log processing.
/// Used by both Dashboard and CLI.
/// </summary>
internal static class SharedAIHelpers
{
    public const int ConsoleLogsLimit = 500;
    public const int MaximumListTokenLength = 8192;
    public const int MaximumStringLength = 2048;

    /// <summary>
    /// Estimates the token count for a string.
    /// This is a rough estimate - use a library for exact calculation.
    /// </summary>
    public static int EstimateTokenCount(string text)
    {
        return text.Length / 4;
    }

    /// <summary>
    /// Serializes a log entry to a string, stripping timestamps and ANSI control sequences.
    /// </summary>
    public static string SerializeLogEntry(LogEntry logEntry)
    {
        if (logEntry.RawContent is not null)
        {
            var content = logEntry.RawContent;
            if (TimestampParser.TryParseConsoleTimestamp(content, out var timestampParseResult))
            {
                content = timestampParseResult.Value.ModifiedText;
            }

            return LimitLength(AnsiParser.StripControlSequences(content));
        }
        else
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Serializes a list of log entry strings to a single string with newlines.
    /// </summary>
    public static string SerializeConsoleLogs(IList<string> logEntries)
    {
        var consoleLogsText = new StringBuilder();

        foreach (var logEntry in logEntries)
        {
            consoleLogsText.AppendLine(logEntry);
        }

        return consoleLogsText.ToString();
    }

    /// <summary>
    /// Limits a string to the maximum length, appending a truncation marker if needed.
    /// </summary>
    public static string LimitLength(string value)
    {
        if (value.Length <= MaximumStringLength)
        {
            return value;
        }

        return
            $"""
            {value.AsSpan(0, MaximumStringLength)}...[TRUNCATED]
            """;
    }

    /// <summary>
    /// Gets items from the end of a list with a summary message, applying count and token limits.
    /// </summary>
    public static (List<object> items, string message) GetLimitFromEndWithSummary<T>(
        List<T> values,
        int limit,
        string itemName,
        string pluralItemName,
        Func<T, object> convertToDto,
        Func<object, int> estimateTokenSize)
    {
        return GetLimitFromEndWithSummary(values, values.Count, limit, itemName, pluralItemName, convertToDto, estimateTokenSize);
    }

    /// <summary>
    /// Gets items from the end of a list with a summary message, applying count and token limits.
    /// </summary>
    public static (List<object> items, string message) GetLimitFromEndWithSummary<T>(
        List<T> values,
        int totalValues,
        int limit,
        string itemName,
        string pluralItemName,
        Func<T, object> convertToDto,
        Func<object, int> estimateTokenSize)
    {
        Debug.Assert(totalValues >= values.Count, "Total values should be large or equal to the values passed into the method.");

        var trimmedItems = values.Count <= limit
            ? values
            : values[^limit..];

        var currentTokenCount = 0;
        var serializedValuesCount = 0;
        var dtos = trimmedItems.Select(i => convertToDto(i)).ToList();

        // Loop backwards to prioritize the latest items.
        for (var i = dtos.Count - 1; i >= 0; i--)
        {
            var obj = dtos[i];
            var tokenCount = estimateTokenSize(obj);

            if (currentTokenCount + tokenCount > MaximumListTokenLength)
            {
                break;
            }

            serializedValuesCount++;
            currentTokenCount += tokenCount;
        }

        // Trim again with what fits in the token limit.
        dtos = dtos[^serializedValuesCount..];

        return (dtos, GetLimitSummary(totalValues, dtos.Count, itemName, pluralItemName));
    }

    /// <summary>
    /// Gets a summary message describing how many items were returned vs total.
    /// </summary>
    public static string GetLimitSummary(int totalValues, int returnedCount, string itemName, string pluralItemName)
    {
        if (totalValues == returnedCount)
        {
            return $"Returned {ToQuantity(returnedCount, itemName, pluralItemName)}.";
        }

        return $"Returned latest {ToQuantity(returnedCount, itemName, pluralItemName)}. Earlier {ToQuantity(totalValues - returnedCount, itemName, pluralItemName)} not returned because of size limits.";
    }

    /// <summary>
    /// Formats an item name with quantity (e.g., "1 console log" or "5 console logs").
    /// </summary>
    private static string ToQuantity(int count, string itemName, string pluralItemName)
    {
        var name = count == 1 ? itemName : pluralItemName;
        return string.Create(CultureInfo.InvariantCulture, $"{count} {name}");
    }
}

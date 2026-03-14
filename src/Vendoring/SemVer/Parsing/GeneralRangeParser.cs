using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Primitives;
using Semver.Utility;

namespace Semver.Parsing;

internal static class GeneralRangeParser
{
    public static int CountSplitOnOrOperator(string range)
    {
        DebugChecks.IsNotNull(range, nameof(range));

        int count = 1; // Always one more item than there are separators
        bool possiblyInSeparator = false;
        // Use `for` instead of `foreach` to ensure performance
        for (int i = 0; i < range.Length; i++)
        {
            var isSeparatorChar = range[i] == '|';
            if (possiblyInSeparator && isSeparatorChar)
            {
                count++;
                possiblyInSeparator = false;
            }
            else
                possiblyInSeparator = isSeparatorChar;
        }

        return count;
    }

    public static IEnumerable<StringSegment> SplitOnOrOperator(string range)
    {
        DebugChecks.IsNotNull(range, nameof(range));

        var possiblyInSeparator = false;
        int start = 0;
        // Use `for` instead of `foreach` to ensure performance
        for (int i = 0; i < range.Length; i++)
        {
            var isSeparatorChar = range[i] == '|';
            if (possiblyInSeparator && isSeparatorChar)
            {
                possiblyInSeparator = false;
                yield return range.Slice(start, i - 1 - start);
                start = i + 1;
            }
            else
                possiblyInSeparator = isSeparatorChar;
        }

        // The final segment from the last separator to the end of the string
        yield return range.Slice(start, range.Length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPossibleOperatorChar(char c, SemVersionRangeOptions rangeOptions)
        => c == '=' || c == '<' || c == '>' || c == '~' || c == '^'
           || (char.IsPunctuation(c) && !char.IsWhiteSpace(c) && c != '*')
           || (char.IsSymbol(c) && (c != '+' || !rangeOptions.HasOption(SemVersionRangeOptions.AllowMetadata)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPossibleVersionChar(char c, SemVersionRangeOptions rangeOptions)
        => !char.IsWhiteSpace(c)
           && (!IsPossibleOperatorChar(c, rangeOptions) || c == '-' || c == '.'
               || (c == '+' && rangeOptions.HasOption(SemVersionRangeOptions.AllowMetadata)));

    /// <summary>
    /// Parse optional spaces from the beginning of the segment.
    /// </summary>
    public static Exception? ParseOptionalSpaces(ref StringSegment segment, Exception? ex)
    {
        segment = segment.TrimStartSpaces();

        if (segment.Length > 0 && char.IsWhiteSpace(segment[0]))
            return ex ?? RangeError.InvalidWhitespace(segment.Offset, segment.Buffer!);
        return null;
    }

    /// <summary>
    /// Parse optional whitespace from the beginning of the segment.
    /// </summary>
    public static void ParseOptionalWhitespace(ref StringSegment segment)
        => segment = segment.TrimStart();

    /// <summary>
    /// Parse a version number from the beginning of the segment.
    /// </summary>
    public static Exception? ParseVersion(
        ref StringSegment segment,
        SemVersionRangeOptions rangeOptions,
        SemVersionParsingOptions parseOptions,
        Exception? ex,
        int maxLength,
        out SemVersion? semver,
        out WildcardVersion wildcardVersion)
    {
        // The SemVersionParser assumes there is nothing following the version number. To reuse
        // its parsing, the appropriate end must be found.
        var end = 0;
        while (end < segment.Length && IsPossibleVersionChar(segment[end], rangeOptions)) end++;
        var version = segment.Subsegment(0, end);
        segment = segment.Subsegment(end);

        var exception = SemVersionParser.Parse(version, rangeOptions.ToStyles(), parseOptions, ex,
            maxLength, out semver, out wildcardVersion);
        if (exception != null) return exception;
        DebugChecks.IsNotNull(semver, nameof(semver));

        // Trim off metadata if it was allowed
        if (semver.MetadataIdentifiers.Count > 0)
            semver = semver.WithoutMetadata();
        return null;
    }
}

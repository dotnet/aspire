using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Primitives;

namespace Semver.Utility;

internal static class StringSegmentExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this StringSegment segment) => segment.Length == 0;

    public static StringSegment TrimStartSpaces(this StringSegment segment)
    {
        var start = 0;
        while (start < segment.Length && segment[start] == ' ') start++;
        return segment.Subsegment(start);
    }

    /// <summary>
    /// Trim leading zeros from a numeric string segment. If the segment consists of all zeros,
    /// return <c>"0"</c>.
    /// </summary>
    /// <remarks>The standard <see cref="string.TrimStart(char[])"/> method handles all zeros
    /// by returning <c>""</c>. This efficiently handles the kind of trimming needed.</remarks>
    public static StringSegment TrimLeadingZeros(this StringSegment segment)
    {
        var start = 0;
        while (start < segment.Length - 1 && segment[start] == '0') start++;
        return segment.Subsegment(start);
    }

    public static string ToStringLimitLength(this StringSegment segment)
    {
        if (segment.Length > Display.Limit) return segment.Subsegment(0, Display.Limit - 3) + "...";

        return segment.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SplitBeforeFirst(this StringSegment segment, char c, out StringSegment left, out StringSegment right)
    {
        var index = segment.IndexOf(c);
        if (index >= 0)
        {
            left = segment.Subsegment(0, index);
            right = segment.Subsegment(index);
        }
        else
        {
            left = segment;
            right = StringSegment.Empty;
        }
    }

    public static int SplitCount(this StringSegment segment, char c)
    {
        int count = 1; // Always one more item than there are separators
        // Use `for` instead of `foreach` to ensure performance
        for (int i = 0; i < segment.Length; i++)
            if (segment[i] == c)
                count++;

        return count;
    }

    /// <remarks>An optimized split for splitting on a single char.</remarks>
    public static IEnumerable<StringSegment> Split(this StringSegment segment, char c)
    {
        var start = 0;
        // Use `for` instead of `foreach` to ensure performance
        for (int i = 0; i < segment.Length; i++)
            if (segment[i] == c)
            {
                yield return segment.Subsegment(start, i - start);
                start = i + 1;
            }

        // The final segment from the last separator to the end of the string
        yield return segment.Subsegment(start);
    }
}

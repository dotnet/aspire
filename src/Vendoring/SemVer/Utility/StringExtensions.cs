using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Primitives;

namespace Semver.Utility;

internal static class StringExtensions
{
    /// <summary>
    /// Is this string composed entirely of ASCII digits '0' to '9'?
    /// </summary>
    public static bool IsDigits(this string value)
    {
        foreach (var c in value)
            if (!c.IsDigit())
                return false;

        return true;
    }

    /// <summary>
    /// Is this string composed entirely of ASCII alphanumeric characters and hyphens?
    /// </summary>
    public static bool IsAlphanumericOrHyphens(this string value)
    {
        foreach (var c in value)
            if (!c.IsAlphaOrHyphen() && !c.IsDigit())
                return false;

        return true;
    }

    /// <summary>
    /// Split a string, map the parts, and return a read only list of the values.
    /// </summary>
    /// <remarks>Splitting a string, mapping the result and storing into a <see cref="IReadOnlyList{T}"/>
    /// is a common operation in this package. This method optimizes that. It avoids the
    /// performance overhead of:
    /// <list type="bullet">
    ///   <item><description>Constructing the params array for <see cref="string.Split(char[])"/></description></item>
    ///   <item><description>Constructing the intermediate <see cref="T:string[]"/> returned by <see cref="string.Split(char[])"/></description></item>
    ///   <item><description><see cref="System.Linq.Enumerable.Select{TSource,TResult}(IEnumerable{TSource},Func{TSource,TResult})"/></description></item>
    ///   <item><description>Not allocating list capacity based on the size</description></item>
    /// </list>
    /// Benchmarking shows this to be 30%+ faster and that may not reflect the whole benefit
    /// since it doesn't fully account for reduced allocations.
    /// </remarks>
    public static IReadOnlyList<T> SplitAndMapToReadOnlyList<T>(
        this string value,
        char splitOn,
        Func<string, T> func)
    {
        if (value.Length == 0) return ReadOnlyList<T>.Empty;

        // Figure out how many items the resulting list will have
        int count = 1; // Always one more item than there are separators
        // Use `for` instead of `foreach` to ensure performance
        for (int i = 0; i < value.Length; i++)
            if (value[i] == splitOn)
                count++;

        // Allocate enough capacity for the items
        var items = new List<T>(count);
        int start = 0;
        for (int i = 0; i < value.Length; i++)
            if (value[i] == splitOn)
            {
                items.Add(func(value.Substring(start, i - start)));
                start = i + 1;
            }
        // Add the final items from the last separator to the end of the string
        items.Add(func(value.Substring(start, value.Length - start)));

        return items.AsReadOnly();
    }

    /// <summary>
    /// Trim leading zeros from a numeric string. If the string consists of all zeros, return
    /// <c>"0"</c>.
    /// </summary>
    /// <remarks>The standard <see cref="string.TrimStart(char[])"/> method handles all zeros
    /// by returning <c>""</c>. This efficiently handles the kind of trimming needed.</remarks>
    public static string TrimLeadingZeros(this string value)
    {
        int start;
        var searchUpTo = value.Length - 1;
        for (start = 0; start < searchUpTo; start++)
            if (value[start] != '0')
                break;

        return value[start..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringSegment Slice(this string value, int offset, int length)
        => new(value, offset, length);

    public static string LimitLength(this string value)
    {
        if (value.Length > Display.Limit)
            return value[..(Display.Limit - 3)] + "...";

        return value;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Extensions;

public static class CollectionExtensions
{
    public static bool Equivalent<T>(this T[] array, T[] other)
    {
        if (array.Length != other.Length)
        {
            return false;
        }

        return !array.Where((t, i) => !Equals(t, other[i])).Any();
    }

    public static T? SingleOrNull<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var enumerator = source.GetEnumerator();

        if (!enumerator.MoveNext())
        {
            return default; // no items
        }

        var first = enumerator.Current;

        if (enumerator.MoveNext())
        {
            return default; // more than one
        }

        return first; // exactly one
    }
}

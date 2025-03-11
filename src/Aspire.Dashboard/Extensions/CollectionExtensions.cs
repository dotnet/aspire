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

    public static bool Equivalent<TKey, TValue>(this Dictionary<TKey, TValue> dict1, Dictionary<TKey, TValue> dict2) where TKey : notnull
    {
        return dict1.Count == dict2.Count && dict1.All(kvp => dict2.TryGetValue(kvp.Key, out var value)
                                                              && EqualityComparer<TValue>.Default.Equals(kvp.Value, value));
    }
}

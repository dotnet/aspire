// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.DotNet.Watch;

internal static class ImmutableDictionaryExtensions
{
    public static ImmutableDictionary<TKey, ImmutableArray<TValue>> Add<TKey, TValue>(this ImmutableDictionary<TKey, ImmutableArray<TValue>> dictionary, TKey key, TValue value)
        where TKey : notnull
    {
        if (!dictionary.TryGetValue(key, out var items))
        {
            items = [];
        }

        return dictionary.SetItem(key, items.Add(value));
    }

    public static ImmutableDictionary<TKey, ImmutableArray<TValue>> Remove<TKey, TValue>(this ImmutableDictionary<TKey, ImmutableArray<TValue>> dictionary, TKey key, TValue value)
        where TKey : notnull
    {
        if (!dictionary.TryGetValue(key, out var items))
        {
            return dictionary;
        }

        var updatedItems = items.Remove(value);
        if (items == updatedItems)
        {
            return dictionary;
        }

        return updatedItems is []
            ? dictionary.Remove(key)
            : dictionary.SetItem(key, updatedItems);
    }

    public static Task ForEachValueAsync<TKey, TValue>(this ImmutableDictionary<TKey, ImmutableArray<TValue>> dictionary, Func<TValue, CancellationToken, Task> action, CancellationToken cancellationToken)
        where TKey : notnull
        => Task.WhenAll(dictionary.SelectMany(entry => entry.Value).Select(project => action(project, cancellationToken))).WaitAsync(cancellationToken);
}

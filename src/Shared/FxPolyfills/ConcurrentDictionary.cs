// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETFRAMEWORK

namespace System.Collections.Concurrent;

internal static partial class FxPolyfillConcurrentDictionary
{
    extension<TKey, TValue>(ConcurrentDictionary<TKey, TValue> dictionary)
    {
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (dictionary.TryGetValue(key, out var existing))
            {
                return existing;
            }

            return dictionary.GetOrAdd(key, valueFactory(key));
        }

        public TValue GetOrAdd<TState>(TKey key, Func<TKey, TState, TValue> valueFactory, TState state)
        {
            if (dictionary.TryGetValue(key, out var existing))
            {
                return existing;
            }

            return dictionary.GetOrAdd(key, valueFactory(key, state));
        }

        public void TryRemove(TKey key)
        {
            dictionary.TryRemove(key, out _);
        }

        public void TryRemove(KeyValuePair<TKey, TValue> pair)
        {
            if (dictionary.TryRemove(pair.Key, out var existing) && !EqualityComparer<TValue>.Default.Equals(existing, pair.Value))
            {
                dictionary.TryAdd(pair.Key, pair.Value);
            }
        }
    }
}

#endif

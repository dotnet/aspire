// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.DebugAdapter.Generator;

/// <summary>
/// Polyfills for netstandard2.0 compatibility.
/// </summary>
internal static class Polyfills
{
    /// <summary>
    /// Deconstructs a KeyValuePair into its key and value components.
    /// </summary>
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
    {
        key = kvp.Key;
        value = kvp.Value;
    }
}

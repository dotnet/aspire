// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Aspire.Hosting.Ats;

/// <summary>
/// ATS exports for dictionary operations.
/// Dictionaries are marshalled as handles to support mutation.
/// </summary>
internal static class DictionaryExports
{
    /// <summary>
    /// Sets a value in a dictionary.
    /// </summary>
    [AspireExport("Dictionary.set", Description = "Sets a value in a dictionary")]
    public static void Set(IDictionary dictionary, string key, object? value)
    {
        dictionary[key] = value;
    }

    /// <summary>
    /// Gets a value from a dictionary.
    /// </summary>
    [AspireExport("Dictionary.get", Description = "Gets a value from a dictionary")]
    public static object? Get(IDictionary dictionary, string key)
    {
        return dictionary.Contains(key) ? dictionary[key] : null;
    }

    /// <summary>
    /// Removes a key from a dictionary.
    /// </summary>
    [AspireExport("Dictionary.remove", Description = "Removes a key from a dictionary")]
    public static bool Remove(IDictionary dictionary, string key)
    {
        if (dictionary.Contains(key))
        {
            dictionary.Remove(key);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if a key exists in a dictionary.
    /// </summary>
    [AspireExport("Dictionary.containsKey", Description = "Checks if a key exists")]
    public static bool ContainsKey(IDictionary dictionary, string key)
    {
        return dictionary.Contains(key);
    }

    /// <summary>
    /// Gets all keys from a dictionary.
    /// </summary>
    [AspireExport("Dictionary.keys", Description = "Gets all keys")]
    public static string[] Keys(IDictionary dictionary)
    {
        var keys = new string[dictionary.Count];
        var i = 0;
        foreach (var key in dictionary.Keys)
        {
            keys[i++] = key?.ToString() ?? "";
        }
        return keys;
    }

    /// <summary>
    /// Gets the count of items in a dictionary.
    /// </summary>
    [AspireExport("Dictionary.count", Description = "Gets the count of items")]
    public static int Count(IDictionary dictionary)
    {
        return dictionary.Count;
    }

    /// <summary>
    /// Clears all items from a dictionary.
    /// </summary>
    [AspireExport("Dictionary.clear", Description = "Clears all items")]
    public static void Clear(IDictionary dictionary)
    {
        dictionary.Clear();
    }
}

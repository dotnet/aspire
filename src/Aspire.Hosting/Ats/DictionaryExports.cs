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
    [AspireExport("aspire/Dictionary.set@1", Description = "Sets a value in a dictionary")]
    public static void Set(IDictionary dictionary, string key, object? value)
    {
        dictionary[key] = value;
    }

    /// <summary>
    /// Gets a value from a dictionary.
    /// </summary>
    [AspireExport("aspire/Dictionary.get@1", Description = "Gets a value from a dictionary")]
    public static object? Get(IDictionary dictionary, string key)
    {
        return dictionary.Contains(key) ? dictionary[key] : null;
    }

    /// <summary>
    /// Removes a key from a dictionary.
    /// </summary>
    [AspireExport("aspire/Dictionary.remove@1", Description = "Removes a key from a dictionary")]
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
    [AspireExport("aspire/Dictionary.containsKey@1", Description = "Checks if a key exists")]
    public static bool ContainsKey(IDictionary dictionary, string key)
    {
        return dictionary.Contains(key);
    }

    /// <summary>
    /// Gets all keys from a dictionary.
    /// </summary>
    [AspireExport("aspire/Dictionary.keys@1", Description = "Gets all keys")]
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
    [AspireExport("aspire/Dictionary.count@1", Description = "Gets the count of items")]
    public static int Count(IDictionary dictionary)
    {
        return dictionary.Count;
    }

    /// <summary>
    /// Clears all items from a dictionary.
    /// </summary>
    [AspireExport("aspire/Dictionary.clear@1", Description = "Clears all items")]
    public static void Clear(IDictionary dictionary)
    {
        dictionary.Clear();
    }
}

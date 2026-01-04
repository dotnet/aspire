// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Aspire.Hosting.Ats;

/// <summary>
/// ATS exports for list operations.
/// Lists are marshalled as handles to support mutation.
/// </summary>
internal static class ListExports
{
    /// <summary>
    /// Adds an item to a list.
    /// </summary>
    [AspireExport("aspire/List.add@1", Description = "Adds an item to a list")]
    public static void Add(IList list, object? item)
    {
        list.Add(item);
    }

    /// <summary>
    /// Gets an item from a list by index.
    /// </summary>
    [AspireExport("aspire/List.get@1", Description = "Gets an item by index")]
    public static object? Get(IList list, int index)
    {
        return index >= 0 && index < list.Count ? list[index] : null;
    }

    /// <summary>
    /// Sets an item in a list by index.
    /// </summary>
    [AspireExport("aspire/List.set@1", Description = "Sets an item by index")]
    public static void Set(IList list, int index, object? item)
    {
        if (index >= 0 && index < list.Count)
        {
            list[index] = item;
        }
    }

    /// <summary>
    /// Removes an item from a list.
    /// </summary>
    [AspireExport("aspire/List.remove@1", Description = "Removes an item from a list")]
    public static void Remove(IList list, object? item)
    {
        list.Remove(item);
    }

    /// <summary>
    /// Removes an item at a specific index.
    /// </summary>
    [AspireExport("aspire/List.removeAt@1", Description = "Removes an item at index")]
    public static void RemoveAt(IList list, int index)
    {
        if (index >= 0 && index < list.Count)
        {
            list.RemoveAt(index);
        }
    }

    /// <summary>
    /// Inserts an item at a specific index.
    /// </summary>
    [AspireExport("aspire/List.insert@1", Description = "Inserts an item at index")]
    public static void Insert(IList list, int index, object? item)
    {
        if (index >= 0 && index <= list.Count)
        {
            list.Insert(index, item);
        }
    }

    /// <summary>
    /// Checks if a list contains an item.
    /// </summary>
    [AspireExport("aspire/List.contains@1", Description = "Checks if list contains item")]
    public static bool Contains(IList list, object? item)
    {
        return list.Contains(item);
    }

    /// <summary>
    /// Gets the index of an item in a list.
    /// </summary>
    [AspireExport("aspire/List.indexOf@1", Description = "Gets the index of an item")]
    public static int IndexOf(IList list, object? item)
    {
        return list.IndexOf(item);
    }

    /// <summary>
    /// Gets the count of items in a list.
    /// </summary>
    [AspireExport("aspire/List.count@1", Description = "Gets the count of items")]
    public static int Count(IList list)
    {
        return list.Count;
    }

    /// <summary>
    /// Clears all items from a list.
    /// </summary>
    [AspireExport("aspire/List.clear@1", Description = "Clears all items")]
    public static void Clear(IList list)
    {
        list.Clear();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Aspire.Dashboard.Components;

public partial class SelectResourceOptions<TValue>
{
    private async Task OnAllValuesCheckedChangedInternalAsync(bool? newAreAllVisible)
    {
        AreAllVisible = newAreAllVisible;
        await AreAllVisibleChanged.InvokeAsync(AreAllVisible);
        await OnAllResourceTypesCheckedChangedAsync();
    }

    private async Task OnValueVisibilityChangedInternalAsync(TValue value, bool isVisible)
    {
        if (isVisible)
        {
            VisibleValues[value] = true;
        }
        else
        {
            VisibleValues.TryRemove(value, out _);
        }

        await OnValueVisibilityChangedAsync(value, isVisible);
    }

    internal static bool? GetFieldVisibility(ConcurrentDictionary<TValue, bool> visibleValues, ConcurrentDictionary<TValue, bool> allValues, StringComparer comparer)
    {
        static bool SetEqualsKeys(ConcurrentDictionary<TValue, bool> left, ConcurrentDictionary<TValue, bool> right, StringComparer comparer)
        {
            // PERF: This is inefficient since Keys locks and copies the keys.
            var keysLeft = left.Keys.Select(key => key.ToString()).ToList();
            var keysRight = right.Keys.Select(key => key.ToString()).ToList();

            return keysLeft.Count == keysRight.Count && keysLeft.OrderBy(key => key, comparer).SequenceEqual(keysRight.OrderBy(key => key, comparer), comparer);
        }

        return SetEqualsKeys(visibleValues, allValues, comparer)
            ? true
            : visibleValues.IsEmpty
                ? false
                : null;
    }

    internal static void SetFieldVisibility(ConcurrentDictionary<TValue, bool> visibleValues, ConcurrentDictionary<TValue, bool> allValues, bool? value, Action stateHasChanged)
    {
        static bool UnionWithKeys(ConcurrentDictionary<TValue, bool> left, ConcurrentDictionary<TValue, bool> right)
        {
            // .Keys locks and copies the keys so avoid it here.
            foreach (var (key, _) in right)
            {
                left[key] = true;
            }

            return true;
        }

        if (value is true)
        {
            UnionWithKeys(visibleValues, allValues);
        }
        else if (value is false)
        {
            visibleValues.Clear();
        }

        stateHasChanged();
    }
}

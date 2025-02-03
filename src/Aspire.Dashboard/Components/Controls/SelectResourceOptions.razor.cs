// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Aspire.Dashboard.Components;

public partial class SelectResourceOptions<TValue>
{
    private async Task OnAllValuesCheckedChangedInternalAsync(bool? newAreAllVisible)
    {
        SetCheckState(newAreAllVisible, Values);
        await OnAllValuesCheckedChangedAsync();
    }

    private Task OnValueVisibilityChangedInternalAsync(TValue value, bool isVisible)
    {
        Values[value] = isVisible;
        return OnValueVisibilityChangedAsync(value, isVisible);
    }

    private static void SetCheckState(bool? newAreAllVisible, ConcurrentDictionary<TValue, bool> values)
    {
        if (newAreAllVisible is null)
        {
            return;
        }

        foreach (var key in values.Keys)
        {
            values[key] = newAreAllVisible.Value;
        }
    }

    private static bool? GetCheckState(ConcurrentDictionary<TValue, bool> values)
    {
        if (values.IsEmpty)
        {
            return true;
        }

        var areAllChecked = true;
        var areAllUnchecked = true;

        foreach (var value in values.Values)
        {
            if (value)
            {
                areAllUnchecked = false;
            }
            else
            {
                areAllChecked = false;
            }
        }

        if (areAllChecked)
        {
            return true;
        }

        if (areAllUnchecked)
        {
            return false;
        }

        return null;
    }
}

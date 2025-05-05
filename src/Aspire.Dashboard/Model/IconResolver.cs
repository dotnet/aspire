// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Model;

public static class IconResolver
{
    private sealed record IconKey(string IconName, IconSize DesiredIconSize, IconVariant IconVariant);
    private static readonly ConcurrentDictionary<IconKey, Icon?> s_iconCache = new();

    public static Icon? ResolveIconName(string iconName, IconSize? desiredIconSize, IconVariant? iconVariant)
    {
        // Icons.GetInstance isn't efficient. Cache icon lookup.
        return s_iconCache.GetOrAdd(new IconKey(iconName, desiredIconSize ?? IconSize.Size20, iconVariant ?? IconVariant.Regular), static key =>
        {
            // Try to get the desired size.
            CustomIcon? icon;
            if (TryGetIcon(key, key.DesiredIconSize, out icon))
            {
                return icon;
            }

            // Some icons aren't available in all sizes. Try until we find a match. Prefer size bigger than desired.
            if (key.DesiredIconSize <= IconSize.Size16 && TryGetIcon(key, IconSize.Size16, out icon))
            {
                return icon;
            }
            if (key.DesiredIconSize <= IconSize.Size20 && TryGetIcon(key, IconSize.Size20, out icon))
            {
                return icon;
            }
            if (key.DesiredIconSize <= IconSize.Size24 && TryGetIcon(key, IconSize.Size24, out icon))
            {
                return icon;
            }

            return null;
        });
    }

    private static bool TryGetIcon(IconKey key, IconSize size, [NotNullWhen(true)] out CustomIcon? icon)
    {
        try
        {
            icon = (new IconInfo
            {
                Name = key.IconName,
                Variant = key.IconVariant,
                Size = size
            }).GetInstance();
            return true;
        }
        catch
        {
            // Icon name or size couldn't be found.
            icon = null;
            return false;
        }
    }
}

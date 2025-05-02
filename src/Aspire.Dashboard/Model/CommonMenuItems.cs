// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Utils;
using Microsoft.Extensions.Localization;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Model;

public static class CommonMenuItems
{
    public static void AddToggleHiddenResourcesMenuItem(
        List<MenuButtonItem> menuItems,
        IStringLocalizer<ControlsStrings> loc,
        bool showHiddenResources,
        IEnumerable<ResourceViewModel> resources,
        Action updateMenuButtons,
        ISessionStorage sessionStorage,
        Func<bool, Task> refreshFunction)
    {
        var areResourcesHidden = resources.Any(r => r.IsHidden(false));
        if (!showHiddenResources)
        {
            menuItems.Add(new MenuButtonItem
            {
                IsDisabled = !areResourcesHidden,
                OnClick = OnToggleShowHiddenResources,
                Text = loc[nameof(ControlsStrings.ShowHiddenResources)],
                Icon = new Icons.Regular.Size16.Eye()
            });
        }
        else
        {
            menuItems.Add(new MenuButtonItem
            {
                OnClick = OnToggleShowHiddenResources,
                Text = loc[nameof(ControlsStrings.HideHiddenResources)],
                Icon = new Icons.Regular.Size16.EyeOff()
            });
        }
        async Task OnToggleShowHiddenResources()
        {
            showHiddenResources = !showHiddenResources;
            await sessionStorage.SetAsync(BrowserStorageKeys.ResourcesShowHiddenResources, showHiddenResources).ConfigureAwait(true);
            await refreshFunction(showHiddenResources).ConfigureAwait(true);
            updateMenuButtons();
        }
    }
}

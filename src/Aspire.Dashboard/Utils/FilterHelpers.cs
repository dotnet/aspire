// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Layout;
using Aspire.Dashboard.Components.Pages;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Resources;
using Microsoft.Extensions.Localization;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Utils;

public static class FilterHelpers
{
    public static IEnumerable<TelemetryFilter> GetEnabledFilters(this IEnumerable<TelemetryFilter> filters)
    {
        return filters.Where(filter => filter.Enabled);
    }

    public static List<MenuButtonItem> GetFilterMenuItems<TView, TR>(
        this IPageWithSessionAndUrlState<TView, TR> page,
        IReadOnlyList<TelemetryFilter> filters,
        Action clearFilters,
        Func<TelemetryFilter, Task> openFilterAsync,
        IStringLocalizer<StructuredFiltering> filterLoc,
        IStringLocalizer<Dialogs> dialogsLoc,
        AspirePageContentLayout? contentLayout) where TR : class
    {
        var filterMenuItems = new List<MenuButtonItem>();

        foreach (var filter in filters)
        {
            filterMenuItems.Add(new MenuButtonItem
            {
                OnClick = () => openFilterAsync(filter),
                Text = filter.GetDisplayText(filterLoc),
                Icon = filter.Enabled ? new Icons.Regular.Size16.Play() : new Icons.Regular.Size16.Pause(),
                Class = "filter-menu-item",
            });
        }

        filterMenuItems.Add(new MenuButtonItem
        {
            IsDivider = true
        });

        if (filters.GetEnabledFilters().Any())
        {
            filterMenuItems.Add(new MenuButtonItem
            {
                Text = dialogsLoc[nameof(Dialogs.FilterDialogDisableAll)],
                Icon = new Icons.Regular.Size16.Pause(),
                OnClick = async () =>
                {
                    foreach (var filter in filters)
                    {
                        filter.Enabled = false;
                    }

                    await page.AfterViewModelChangedAsync(contentLayout, waitToApplyMobileChange: false).ConfigureAwait(true);
                }
            });
        }
        else
        {
            filterMenuItems.Add(new MenuButtonItem
            {
                Text = dialogsLoc[nameof(Dialogs.FilterDialogEnableAll)],
                Icon = new Icons.Regular.Size16.Play(),
                OnClick = async () =>
                {
                    foreach (var filter in filters)
                    {
                        filter.Enabled = true;
                    }

                    await page.AfterViewModelChangedAsync(contentLayout, waitToApplyMobileChange: false).ConfigureAwait(true);
                }
            });
        }

        filterMenuItems.Add(new MenuButtonItem
        {
            Text = dialogsLoc[nameof(Dialogs.SettingsRemoveAllButtonText)],
            Icon = new Icons.Regular.Size16.Delete(),
            OnClick = async () =>
            {
                clearFilters();
                await page.AfterViewModelChangedAsync(contentLayout, waitToApplyMobileChange: false).ConfigureAwait(true);
            }
        });

        return filterMenuItems;
    }
}

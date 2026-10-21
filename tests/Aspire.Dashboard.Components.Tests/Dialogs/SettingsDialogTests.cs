// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Telemetry;
using Aspire.Dashboard.Utils;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Dialogs;

[UseCulture("en-US")]
public sealed class SettingsDialogTests : DashboardTestContext
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Render_TelemetryNoticeFollowsConfiguredEnablement(bool enabled)
    {
        var themeManager = new ThemeManager(new TestThemeResolver());
        await themeManager.EnsureInitializedAsync();
        FluentUISetupHelpers.AddCommonDashboardServices(this, themeManager: themeManager);
        FluentUISetupHelpers.SetupFluentList(this);
        Services.AddSingleton(new DashboardTelemetryConfiguration { ReportedTelemetryEnabled = enabled });

        var cut = Render<SettingsDialog>();

        Assert.Equal(enabled ? 1 : 0, cut.FindAll("a[href='https://aka.ms/aspire/dashboard-microsoft-collected-telemetry']").Count);
    }

    [Fact]
    public async Task Render_RadioLabelsUseLocalizedResources()
    {
        var themeManager = new ThemeManager(new TestThemeResolver());
        await themeManager.EnsureInitializedAsync();
        FluentUISetupHelpers.AddCommonDashboardServices(this, themeManager: themeManager);
        FluentUISetupHelpers.SetupFluentList(this);

        var cut = Render<SettingsDialog>();

        Assert.Collection(
            cut.FindAll("fluent-radio-group label"),
            label => Assert.Equal("System", label.TextContent),
            label => Assert.Equal("Light", label.TextContent),
            label => Assert.Equal("Dark", label.TextContent),
            label => Assert.Equal("System", label.TextContent),
            label => Assert.Equal("12-hour", label.TextContent),
            label => Assert.Equal("24-hour", label.TextContent));
    }

    [Fact]
    public async Task ViewPreferences_LoadStoredValuesAndApplyAndStoreChanges()
    {
        var themeManager = new ThemeManager(new TestThemeResolver());
        await themeManager.EnsureInitializedAsync();
        var stored = new Dictionary<string, object?>();
        var localStorage = new TestLocalStorage
        {
            OnGetUnprotectedAsync = key => key switch
            {
                BrowserStorageKeys.ResourceListDensity => (true, "Compact"),
                BrowserStorageKeys.GroupOverview => (true, "Resources"),
                BrowserStorageKeys.RecordingsStyle => (true, "Menu"),
                _ => (false, null)
            },
            OnSetUnprotectedAsync = (key, value) => stored[key] = value
        };
        FluentUISetupHelpers.AddCommonDashboardServices(this, themeManager: themeManager, localStorage: localStorage);
        FluentUISetupHelpers.SetupFluentList(this);
        var preferences = Services.GetRequiredService<DashboardViewPreferences>();

        var cut = Render<SettingsDialog>();

        cut.WaitForAssertion(() => Assert.Equal(["Compact", "Resource list", "Live run menu"], cut.FindAll(".icon-toggle-current").Select(e => e.TextContent)));
        Assert.Equal(
            ["Comfortable", "Compact", "Summary", "Resource list", "Recordings panel", "Live run menu"],
            cut.FindAll(".icon-toggle").Select(e => e.GetAttribute("aria-label")));
        Assert.Equal(["Compact", "Resource list", "Live run menu"], cut.FindAll(".icon-toggle[aria-pressed='true']").Select(e => e.GetAttribute("aria-label")));

        await cut.InvokeAsync(() => cut.Find(".icon-toggle[aria-label='Comfortable']").Click());
        await cut.InvokeAsync(() => cut.Find(".icon-toggle[aria-label='Summary']").Click());
        await cut.InvokeAsync(() => cut.Find(".icon-toggle[aria-label='Recordings panel']").Click());

        Assert.Equal(ResourceListDensity.Comfortable, preferences.Density);
        Assert.Equal(GroupOverview.Summary, preferences.GroupOverview);
        Assert.Equal("Comfortable", stored[BrowserStorageKeys.ResourceListDensity]);
        Assert.Equal("Summary", stored[BrowserStorageKeys.GroupOverview]);
        Assert.Equal(RecordingsStyle.Panel, preferences.RecordingsStyle);
        Assert.Equal("Panel", stored[BrowserStorageKeys.RecordingsStyle]);
        Assert.Equal(["Comfortable", "Summary", "Recordings panel"], cut.FindAll(".icon-toggle[aria-pressed='true']").Select(e => e.GetAttribute("aria-label")));
    }
}

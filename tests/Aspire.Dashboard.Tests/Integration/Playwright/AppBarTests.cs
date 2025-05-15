// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Tests.Integration.Playwright.Infrastructure;
using Aspire.TestUtilities;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Playwright;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration.Playwright;

[RequiresPlaywright]
public class AppBarTests : PlaywrightTestsBase<DashboardServerFixture>
{
    public AppBarTests(DashboardServerFixture dashboardServerFixture)
        : base(dashboardServerFixture)
    {
    }

    [Fact]
    public async Task AppBar_Change_Theme()
    {
        // Arrange
        await RunTestAsync(async page =>
        {
            await PlaywrightFixture.GoToHomeAndWaitForDataGridLoad(page).DefaultTimeout();

            await SetAndVerifyTheme(Dialogs.SettingsDialogSystemTheme, null).DefaultTimeout(); // don't guess system theme
            await SetAndVerifyTheme(Dialogs.SettingsDialogLightTheme, "light").DefaultTimeout();
            await SetAndVerifyTheme(Dialogs.SettingsDialogDarkTheme, "dark").DefaultTimeout();

            async Task SetAndVerifyTheme(string checkboxText, string? expected)
            {
                var settingsButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = Layout.MainLayoutLaunchSettings });
                await settingsButton.ClickAsync();

                // Set theme
                var checkbox = page.GetByRole(AriaRole.Radio).And(page.GetByText(checkboxText)).First;
                await checkbox.ClickAsync();

                if (expected != null)
                {
                    await Assertions
                        .Expect(page.Locator("html"))
                        .ToHaveAttributeAsync("data-theme", expected);
                }

                // Close settings.
                var closeButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = Layout.MainLayoutSettingsDialogClose });
                await closeButton.First.ClickAsync();

                // Re-open settings and assert that the correct checkbox is checked.
                await settingsButton.ClickAsync();

                checkbox = page.GetByRole(AriaRole.Radio).And(page.GetByText(checkboxText)).First;

                await AsyncTestHelpers.AssertIsTrueRetryAsync(
                    async () => await checkbox.IsCheckedAsync(),
                    "Checkbox isn't immediately checked.");

                await closeButton.First.ClickAsync();
            }
        });
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/9152", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOS))]
    public async Task AppBar_Change_Theme_ReloadPage()
    {
        // Arrange
        await RunTestAsync(async page =>
        {
            await SetAndVerifyTheme(Dialogs.SettingsDialogSystemTheme, null).DefaultTimeout(); // don't guess system theme
            await SetAndVerifyTheme(Dialogs.SettingsDialogLightTheme, "light").DefaultTimeout();
            await SetAndVerifyTheme(Dialogs.SettingsDialogDarkTheme, "dark").DefaultTimeout();

            async Task SetAndVerifyTheme(string checkboxText, string? expected)
            {
                await PlaywrightFixture.GoToHomeAndWaitForDataGridLoad(page);

                var settingsButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = Layout.MainLayoutLaunchSettings });
                await settingsButton.ClickAsync();

                // Set theme
                var checkbox = page.GetByRole(AriaRole.Radio).And(page.GetByText(checkboxText)).First;
                await checkbox.ClickAsync();

                if (expected != null)
                {
                    await Assertions
                        .Expect(page.Locator("html"))
                        .ToHaveAttributeAsync("data-theme", expected);
                }

                // Reload page.
                await PlaywrightFixture.GoToHomeAndWaitForDataGridLoad(page);

                // Re-open settings and assert that the correct checkbox is checked.
                settingsButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = Layout.MainLayoutLaunchSettings });
                await settingsButton.ClickAsync();

                checkbox = page.GetByRole(AriaRole.Radio).And(page.GetByText(checkboxText)).First;

                await AsyncTestHelpers.AssertIsTrueRetryAsync(
                    async () => await checkbox.IsCheckedAsync(),
                    "Checkbox isn't immediately checked.",
                    retries: 15 /* this seems to take a very long time to run, so needs a long timeout */);
            }
        });
    }
}

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
    [OuterloopTest("Resource-intensive Playwright browser test")]
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
    [OuterloopTest("Resource-intensive Playwright browser test")]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/9152")]
    public async Task AppBar_Change_Theme_ReloadPage()
    {
        // Arrange
        await RunTestAsync(async page =>
        {
            await SetAndVerifyTheme(Dialogs.SettingsDialogSystemTheme, null); // don't guess system theme
            await SetAndVerifyTheme(Dialogs.SettingsDialogLightTheme, "light");
            await SetAndVerifyTheme(Dialogs.SettingsDialogDarkTheme, "dark");

            async Task SetAndVerifyTheme(string checkboxText, string? expected)
            {
                await PlaywrightFixture.GoToHomeAndWaitForDataGridLoad(page).DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

                var settingsButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = Layout.MainLayoutLaunchSettings });
                await settingsButton.ClickAsync().DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

                // Set theme
                var checkbox = page.GetByRole(AriaRole.Radio).And(page.GetByText(checkboxText)).First;
                await checkbox.ClickAsync().DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

                if (expected != null)
                {
                    await Assertions
                        .Expect(page.Locator("html"))
                        .ToHaveAttributeAsync("data-theme", expected);
                }

                // Close the dialog before reloading to ensure the theme change is fully processed.
                var closeButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = Layout.MainLayoutSettingsDialogClose });
                await closeButton.First.ClickAsync().DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

                // Reload page.
                await PlaywrightFixture.GoToHomeAndWaitForDataGridLoad(page).DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

                // Re-open settings and assert that the correct checkbox is checked.
                settingsButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = Layout.MainLayoutLaunchSettings });
                await settingsButton.ClickAsync().DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

                checkbox = page.GetByRole(AriaRole.Radio).And(page.GetByText(checkboxText)).First;

                await Assertions
                    .Expect(checkbox)
                    .ToBeCheckedAsync();
            }
        });
    }
}

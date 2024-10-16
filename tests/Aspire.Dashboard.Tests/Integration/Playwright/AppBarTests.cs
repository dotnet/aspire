// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Tests.Integration.Playwright.Infrastructure;
using Aspire.Workload.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Playwright;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration.Playwright;

[ActiveIssue("https://github.com/dotnet/aspire/issues/4623", typeof(PlaywrightProvider), nameof(PlaywrightProvider.DoesNotHavePlaywrightSupport))]
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
            await PlaywrightFixture.GoToHomeAndWaitForDataGridLoad(page);

            await SetAndVerifyTheme(Dialogs.SettingsDialogDarkTheme, "dark");
            await SetAndVerifyTheme(Dialogs.SettingsDialogLightTheme, "light");

            async Task SetAndVerifyTheme(string checkboxText, string expected)
            {
                var settingsButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = Layout.MainLayoutLaunchSettings });
                await settingsButton.ClickAsync();

                // Set theme
                var checkbox = page.GetByRole(AriaRole.Radio).And(page.GetByText(checkboxText)).First;
                await checkbox.ClickAsync();

                await Assertions
                    .Expect(page.Locator("html"))
                    .ToHaveAttributeAsync("data-theme", expected);

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
}

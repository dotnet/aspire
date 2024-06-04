// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Resources;
using Microsoft.Playwright;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration.Playwright;

public class AppBarTests(DashboardServerFixture dashboardServerFixture, PlaywrightFixture playwrightFixture) : IClassFixture<DashboardServerFixture>, IClassFixture<PlaywrightFixture>
{
    [Fact]
    public async Task AppBar_Change_Theme()
    {
        // Arrange
        var page = await playwrightFixture.Browser.NewPageAsync(new BrowserNewPageOptions { BaseURL = dashboardServerFixture.DashboardApp.FrontendEndPointAccessor().Address});
        await playwrightFixture.GoToHomeAndWaitForDataGridLoad(page);

        var settingsButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
        {
            Name = Layout.MainLayoutLaunchSettings
        });

        await settingsButton.ClickAsync();

        // Act and Assert

        // set to dark
        var darkThemeCheckbox = page.GetByRole(AriaRole.Radio).And(page.GetByText(Dialogs.SettingsDialogDarkTheme)).First;
        var lightThemeCheckbox = page.GetByRole(AriaRole.Radio).And(page.GetByText(Dialogs.SettingsDialogLightTheme)).First;

        await SetAndVerifyTheme(darkThemeCheckbox, "dark");
        await SetAndVerifyTheme(lightThemeCheckbox, "light");

        return;

        async Task SetAndVerifyTheme(ILocator locator, string expected)
        {
            await locator.ClickAsync();
            await Task.Delay(500);

            Assert.Equal(expected, await page.Locator("html").First.GetAttributeAsync("data-theme"));
        }
    }
}


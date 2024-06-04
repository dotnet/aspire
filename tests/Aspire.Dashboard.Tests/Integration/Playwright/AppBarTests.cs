// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration.Playwright;

public class AppBarTests(DashboardServerFixture dashboardServerFixture, PlaywrightFixture playwrightFixture) : IClassFixture<DashboardServerFixture>, IClassFixture<PlaywrightFixture>
{
    [Fact]
    public async Task Post_DeleteAllMessagesHandler_ReturnsRedirectToRoot()
    {
        // Arrange
        var page = await playwrightFixture.Browser.NewPageAsync(new BrowserNewPageOptions { BaseURL = dashboardServerFixture.DashboardApp.FrontendEndPointAccessor().Address});
        await page.GotoAsync("/");

        // Act and Assert
        var settingsButton = page.GetByRole(AriaRole.Button, new()
        {
            Name = "Launch settings"
        });
        await settingsButton.ClickAsync();
        var darkThemeCheckbox = page.GetByRole(AriaRole.Radio).And(page.GetByText("Dark")).First;
        await darkThemeCheckbox.ClickAsync();
        Assert.Equal("dark", await page.Locator("html").First.GetAttributeAsync("data-theme"));

        await Task.Delay(20000);

    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace Aspire.Hosting.Tests.Dashboard;

public class AppbarTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _playwrightFixture;

    public AppbarTests(PlaywrightFixture playwrightFixture)
    {
        _playwrightFixture = playwrightFixture;
    }

    [LocalOnlyFact]
    public async Task Dashboard_Test_Change_Theme()
    {
        var (_, page) = await _playwrightFixture.SetupDashboardForPlaywrightAsync();
        var settingsButton = page.GetByRole(AriaRole.Button, new()
        {
            Name = "Launch settings"
        });
        await settingsButton.ClickAsync();
        var darkThemeCheckbox = page.GetByRole(AriaRole.Radio).And(page.GetByText("Dark")).First;
        await darkThemeCheckbox.ClickAsync();
        Assert.Equal("dark", await page.Locator("html").First.GetAttributeAsync("data-theme"));
    }
}

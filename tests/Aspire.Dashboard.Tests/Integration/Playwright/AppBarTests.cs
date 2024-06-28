// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Resources;
using Aspire.Workload.Tests;
using Microsoft.Playwright;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration.Playwright;

[ActiveIssue("https://github.com/dotnet/aspire/issues/4623", typeof(PlaywrightProvider), nameof(PlaywrightProvider.DoesNotHavePlaywrightSupport))]
public class AppBarTests : PlaywrightTestsBase
{
    public AppBarTests(DashboardServerFixture dashboardServerFixture, PlaywrightFixture playwrightFixture)
        : base(dashboardServerFixture, playwrightFixture)
    {
    }

    [Fact]
    public async Task AppBar_Change_Theme()
    {
        // Arrange
        await RunTestAsync(async page =>
        {
            await PlaywrightFixture.GoToHomeAndWaitForDataGridLoad(page);

            var settingsButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = Layout.MainLayoutLaunchSettings });

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
                await Assertions
                    .Expect(page.Locator("html"))
                    .ToHaveAttributeAsync("data-theme", expected);
            }
        });
    }
}

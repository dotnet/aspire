// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Tests.Integration.Playwright.Infrastructure;
using Aspire.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Playwright;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration.Playwright;

[RequiresPlaywright]
public class BrowserTokenAuthenticationTests : PlaywrightTestsBase<BrowserTokenAuthenticationTests.BrowserTokenDashboardServerFixture>
{
    public class BrowserTokenDashboardServerFixture : DashboardServerFixture
    {
        public BrowserTokenDashboardServerFixture()
        {
            Configuration[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = nameof(FrontendAuthMode.BrowserToken);
            Configuration[DashboardConfigNames.DashboardFrontendBrowserTokenName.ConfigKey] = "VALID_TOKEN";
        }
    }

    public BrowserTokenAuthenticationTests(BrowserTokenDashboardServerFixture dashboardServerFixture)
        : base(dashboardServerFixture)
    {
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/7921", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnGithubActions), nameof(PlatformDetection.IsWindows))]
    public async Task BrowserToken_LoginPage_Success_RedirectToResources()
    {
        // Arrange
        await RunTestAsync(async page =>
        {
            // Act
            var response = await page.GotoAsync("/").DefaultTimeout();
            var uri = new Uri(response!.Url);

            Assert.Equal("/login?returnUrl=%2F", uri.PathAndQuery);

            var tokenTextBox = page.GetByRole(AriaRole.Textbox);
            await tokenTextBox.FillAsync("VALID_TOKEN").DefaultTimeout();

            var submitButton = page.GetByRole(AriaRole.Button);
            await submitButton.ClickAsync().DefaultTimeout();

            // Assert
            await Assertions
                .Expect(page.GetByText(MockDashboardClient.TestResource1.DisplayName))
                .ToBeVisibleAsync()
                .DefaultTimeout();
        });
    }

    [Fact(Skip = "https://github.com/dotnet/aspire/issues/7522")]
    public async Task BrowserToken_LoginPage_Failure_DisplayFailureMessage()
    {
        // Arrange
        await RunTestAsync(async page =>
        {
            // Act
            var response = await page.GotoAsync("/").DefaultTimeout();
            var uri = new Uri(response!.Url);

            Assert.Equal("/login?returnUrl=%2F", uri.PathAndQuery);

            var tokenTextBox = page.GetByRole(AriaRole.Textbox);
            await tokenTextBox.FillAsync("INVALID_TOKEN").DefaultTimeout();

            var submitButton = page.GetByRole(AriaRole.Button);
            await submitButton.ClickAsync().DefaultTimeout();

            // Assert
            const int pageVisibleTimeout = 10000;

            await Assertions
                .Expect(page.GetByText("Invalid token"))
                .ToBeVisibleAsync()
                .DefaultTimeout(pageVisibleTimeout);
        });
    }

    [Fact]
    public async Task BrowserToken_QueryStringToken_Success_RestrictToResources()
    {
        // Arrange
        await RunTestAsync(async page =>
        {
            // Act
            await page.GotoAsync("/login?t=VALID_TOKEN").DefaultTimeout();

            // Assert
            await Assertions
                .Expect(page.GetByText(MockDashboardClient.TestResource1.DisplayName))
                .ToBeVisibleAsync()
                .DefaultTimeout();
        });
    }

    [Fact]
    public async Task BrowserToken_QueryStringToken_Failure_DisplayLoginPage()
    {
        // Arrange
        await RunTestAsync(async page =>
        {
            // Act
            await page.GotoAsync("/login?t=INVALID_TOKEN").DefaultTimeout();

            var submitButton = page.GetByRole(AriaRole.Button);
            var name = await submitButton.GetAttributeAsync("name").DefaultTimeout();

            // Assert
            Assert.Equal("submit-token", name);
        });
    }
}

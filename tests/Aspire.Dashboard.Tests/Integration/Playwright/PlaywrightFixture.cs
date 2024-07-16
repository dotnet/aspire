// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Workload.Tests;
using Microsoft.Playwright;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration.Playwright;

public class PlaywrightFixture : IAsyncLifetime
{
    public IBrowser Browser { get; set; } = null!;

    public async Task InitializeAsync()
    {
        PlaywrightProvider.DetectAndSetInstalledPlaywrightDependenciesPath();
        Browser = await PlaywrightProvider.CreateBrowserAsync();
    }

    public async Task DisposeAsync()
    {
        await Browser.CloseAsync();
    }

    public static async Task GoToHomeAndWaitForDataGridLoad(IPage page)
    {
        await page.GotoAsync("/");
        await Assertions
            .Expect(page.GetByText(MockDashboardClient.TestResource1.DisplayName))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 45_000 });
    }
}

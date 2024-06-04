// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration.Playwright;

public class PlaywrightFixture : IAsyncLifetime
{
    public IBrowser Browser { get; set; } = null!;
    private IPlaywright PlaywrightInstance { get; set; } = null!;

    public async Task InitializeAsync()
    {
        PlaywrightInstance = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
    }

    public async Task DisposeAsync()
    {
        await Browser.CloseAsync();
    }
}

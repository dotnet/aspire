// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration.Playwright.Infrastructure;

public class PlaywrightTestsBase<TDashboardServerFixture> : IClassFixture<TDashboardServerFixture>, IAsyncDisposable
    where TDashboardServerFixture : DashboardServerFixture
{
    public DashboardServerFixture DashboardServerFixture { get; }
    public PlaywrightFixture PlaywrightFixture { get; }

    private IBrowserContext? _context;

    public PlaywrightTestsBase(DashboardServerFixture dashboardServerFixture)
    {
        DashboardServerFixture = dashboardServerFixture;
        PlaywrightFixture = dashboardServerFixture.PlaywrightFixture;
    }

    public async Task RunTestAsync(Func<IPage, Task> test)
    {
        var page = await CreateNewPageAsync();
        try
        {
            await test(page);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private async Task<IPage> CreateNewPageAsync()
    {
        _context ??= await PlaywrightFixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            BaseURL = DashboardServerFixture.DashboardApp.FrontendSingleEndPointAccessor().GetResolvedAddress()
        });

        return await _context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_context is not null)
        {
            await _context.DisposeAsync();
        }
    }
}

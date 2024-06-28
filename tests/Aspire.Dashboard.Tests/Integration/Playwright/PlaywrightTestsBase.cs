// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Workload.Tests;
using Microsoft.Playwright;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration.Playwright;

public class PlaywrightTestsBase : IClassFixture<DashboardServerFixture>, IClassFixture<PlaywrightFixture>, IAsyncDisposable
{
    public DashboardServerFixture DashboardServerFixture { get; }
    public PlaywrightFixture PlaywrightFixture { get; }

    private IBrowserContext? _context;

    public PlaywrightTestsBase(DashboardServerFixture dashboardServerFixture, PlaywrightFixture playwrightFixture)
    {
        DashboardServerFixture = dashboardServerFixture;
        PlaywrightFixture = playwrightFixture;
    }

    public async Task RunTestAsync(Func<IPage, Task> test)
    {
        var page = await CreateNewPageAsync();

        if (page is not null)
        {
            try
            {
                await test(page);
            }
            finally
            {
                await page.CloseAsync();
            }
        }
    }

    private async Task<IPage?> CreateNewPageAsync()
    {
        _context ??= BuildEnvironment.HasPlaywrightSupport
            ? await PlaywrightFixture.Browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true,
                BaseURL = DashboardServerFixture.DashboardApp.FrontendEndPointAccessor().Address
            })
            : null;

        if (_context is not null)
        {
            return await _context.NewPageAsync();
        }

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }
    }
}

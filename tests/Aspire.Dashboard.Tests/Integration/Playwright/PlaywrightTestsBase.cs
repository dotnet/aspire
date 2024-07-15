// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Dashboard.Tests.Integration.Playwright;

public class PlaywrightTestsBase : IClassFixture<DashboardServerFixture>, IClassFixture<PlaywrightFixture>, IAsyncDisposable
{
    public DashboardServerFixture DashboardServerFixture { get; }
    public PlaywrightFixture PlaywrightFixture { get; }

    private readonly ITestOutputHelper _output;
    private IBrowserContext? _context;

    public PlaywrightTestsBase(DashboardServerFixture dashboardServerFixture, PlaywrightFixture playwrightFixture, ITestOutputHelper output)
    {
        DashboardServerFixture = dashboardServerFixture;
        PlaywrightFixture = playwrightFixture;
        _output = output;
    }

    public async Task RunTestAsync(Func<IPage, Task<bool>> setup, Func<IPage, Task> test)
    {
        var page = await CreateNewPageAsync();
        try
        {
            var shouldRunTest = true;

            try
            {
                await setup(page);
            }
            catch (PlaywrightException e)
            {
                _output.WriteLine($"Test setup failed: {e}");
                shouldRunTest = false;
            }

            if (shouldRunTest)
            {
                await test(page);
            }
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
            BaseURL = DashboardServerFixture.DashboardApp.FrontendEndPointAccessor().Address
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

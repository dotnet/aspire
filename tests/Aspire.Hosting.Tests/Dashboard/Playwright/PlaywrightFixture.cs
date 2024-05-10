// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
using Xunit;

namespace Aspire.Hosting.Tests.Dashboard;

public class PlaywrightFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        PlaywrightInstance = await Playwright.CreateAsync();
        Browser = await PlaywrightInstance.Chromium.LaunchAsync(new() { Headless = false });
    }

    public IBrowser Browser { get; set; } = null!;
    private IPlaywright PlaywrightInstance { get; set; } = null!;

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        PlaywrightInstance.Dispose();
    }

    private sealed class MockDashboardClient : IDashboardClient
    {
        public bool IsEnabled => true;
        public Task WhenConnected => Task.CompletedTask;
        public string ApplicationName => "<marquee>An HTML title!</marquee>";
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public Task<ResourceCommandResponseViewModel> ExecuteResourceCommandAsync(string resourceName, string resourceType, CommandViewModel command, CancellationToken cancellationToken) => throw new NotImplementedException();
        public IAsyncEnumerable<IReadOnlyList<ResourceLogLine>>? SubscribeConsoleLogs(string resourceName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ResourceViewModelSubscription> SubscribeResourcesAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    public async Task<(string DashboardUrl, IPage Page)> SetupDashboardForPlaywrightAsync()
    {

        var url = "http://localhost:1234";

        var args = new string[] {
            "ASPNETCORE_ENVIRONMENT=Development",
            "DOTNET_ENVIRONMENT=Development",
            $"ASPNETCORE_URLS={url}",
            $"DOTNET_DASHBOARD_OTLP_ENDPOINT_URL={url}5",
            "DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true",
            "DOTNET_ASPIRE_SHOW_DASHBOARD_RESOURCES=true"
        };

        using var testProgram = TestProgram.Create<PlaywrightFixture>(
            args,
            includeIntegrationServices: false,
            disableDashboard: false);
        var services = testProgram.AppBuilder.Services;
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<IDashboardClient, MockDashboardClient>();

        await using var app = testProgram.Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        await app.StartAsync(cts.Token);
        var page = await Browser.NewPageAsync();
        await WaitUntilDashboardReadyAsync();

        return (url, page);

        async Task WaitUntilDashboardReadyAsync()
        {
            try
            {
                await page.GotoAsync(url);
            }
            catch (Exception)
            {
                await Task.Delay(1000, cts.Token);
                await WaitUntilDashboardReadyAsync();
            }
        }
    }
}

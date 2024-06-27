﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Workload.Tests;
using Microsoft.Playwright;
using Polly;
using Polly.Retry;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration.Playwright;

public class PlaywrightFixture : IAsyncLifetime
{
    public IBrowser Browser { get; set; } = null!;

    private ResiliencePipeline _resiliencePipeline = null!;

    public async Task InitializeAsync()
    {
        PlaywrightProvider.DetectAndSetInstalledPlaywrightDependenciesPath();
        Browser = await PlaywrightProvider.CreateBrowserAsync();

        var retryOptions = new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<ArgumentException>(),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            MaxRetryAttempts = 20,
            Delay = TimeSpan.FromMilliseconds(100),
        };

        var resiliencePipelineBuilder = new ResiliencePipelineBuilder();
        resiliencePipelineBuilder
            .AddRetry(retryOptions)
            .AddTimeout(TimeSpan.FromSeconds(10));
        _resiliencePipeline = resiliencePipelineBuilder.Build();
    }

    public async Task DisposeAsync()
    {
        await Browser.CloseAsync();
    }

    public async Task GoToHomeAndWaitForDataGridLoad(IPage page)
    {
        await page.GotoAsync("/");

        await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            if (await page.GetByText(MockDashboardClient.TestResource1.DisplayName).CountAsync() == 0)
            {
                throw new ArgumentException("Data grid has not loaded yet");
            }

        });
    }
}

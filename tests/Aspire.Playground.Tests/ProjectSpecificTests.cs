// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.Tests.Utils;
using Aspire.Components.Common.Tests;
using SamplesIntegrationTests;
using SamplesIntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Playground.Tests;

public class ProjectSpecificTests(ITestOutputHelper _testOutput)
{
    [Fact]
    public async Task WithDockerfileTest()
    {
        var appHostPath = Directory.GetFiles(AppContext.BaseDirectory, "WithDockerfile.AppHost.dll").Single();
        var appHost = await DistributedApplicationTestFactory.CreateAsync(appHostPath, _testOutput);
        await using var app = await appHost.BuildAsync();

        await app.StartAsync();
        await app.WaitForResources().WaitAsync(TimeSpan.FromMinutes(2));

        await app.WaitForTextAsync($"I'm Batman. - Batman")
                .WaitAsync(TimeSpan.FromMinutes(3));

        app.EnsureNoErrorsLogged();
        await app.StopAsync();
    }

    [Fact]
    public async Task KafkaTest()
    {
        var appHostPath = Directory.GetFiles(AppContext.BaseDirectory, "KafkaBasic.AppHost.dll").Single();
        var appHost = await DistributedApplicationTestFactory.CreateAsync(appHostPath, _testOutput);
        await using var app = await appHost.BuildAsync();

        await app.StartAsync();
        await app.WaitForResources().WaitAsync(TimeSpan.FromMinutes(2));

        // Wait for the producer to start sending messages
        await app.WaitForTextAsync("Hello, World!").WaitAsync(TimeSpan.FromMinutes(5));

        // Wait for the consumer to receive some messages
        await WaitForAllTextAsync(app,
            [
                "Hello, World! 343",
                "Received 1000 messages."
            ],
            timeoutSecs: 120);

        app.EnsureNoErrorsLogged();
        await app.StopAsync();
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/5960")]
    [RequiresDocker]
    [RequiresTools(["func"])]
    public async Task AzureFunctionsTest()
    {
        var appHostPath = Directory.GetFiles(AppContext.BaseDirectory, "AzureFunctionsEndToEnd.AppHost.dll").Single();
        var appHost = await DistributedApplicationTestFactory.CreateAsync(appHostPath, _testOutput);
        await using var app = await appHost.BuildAsync();

        await app.StartAsync();
        await app.WaitForResources().WaitAsync(TimeSpan.FromMinutes(2));

        // Wait for the blobTrigger to be discovered as an indication that the host and worker
        // has been successfully launched
        await WaitForAllTextAsync(app,
            [
                "MyAzureBlobTrigger: blobTrigger"
            ],
            resourceName: "funcapp",
            timeoutSecs: 160);

        // Assert that HTTP triggers work correctly
        await app.CreateHttpClient("funcapp").GetAsync("/api/weatherforecast");
        await WaitForAllTextAsync(app,
            [
                "Executing HTTP request:",
                "api/weatherforecast"
            ],
            resourceName: "funcapp",
            timeoutSecs: 160);

        // Assert that Azure Storage Queue triggers work correctly
        await app.CreateHttpClient("apiservice").GetAsync("/publish/asq");
        await WaitForAllTextAsync(app,
            [
                "Executed 'Functions.MyAzureQueueTrigger'"
            ],
            resourceName: "funcapp",
            timeoutSecs: 160);

        // Assert that Azure Storage Blob triggers work correctly
        await app.CreateHttpClient("apiservice").GetAsync("/publish/blob");
        await WaitForAllTextAsync(app,
            [
                "Executed 'Functions.MyAzureBlobTrigger'"
            ],
            resourceName: "funcapp",
            timeoutSecs: 160);

#if !SKIP_EVENTHUBS_EMULATION
        // Assert that EventHubs triggers work correctly
        await app.CreateHttpClient("apiservice").GetAsync("/publish/eventhubs");
        await WaitForAllTextAsync(app,
            [
                "Executed 'Functions.MyEventHubTrigger'"
            ],
            resourceName: "funcapp",
            timeoutSecs: 160);

        // Assert that ServiceBus triggers work correctly
        await app.CreateHttpClient("apiservice").GetAsync("/publish/asb");
        await WaitForAllTextAsync(app,
            [
                "Executed 'Functions.MyServiceBusTrigger'"
            ],
            resourceName: "funcapp",
            timeoutSecs: 160);
#endif

        // TODO: The following line is commented out because the test fails due to an erroneous log in the Functions App
        // resource that happens after the Functions host has been built. The error log shows up after the Functions
        // worker extension has been built and before the host has launched.
        // app.EnsureNoErrorsLogged();
        await app.StopAsync();
    }

    internal static Task WaitForAllTextAsync(DistributedApplication app, IEnumerable<string> logTexts, string? resourceName = null, int timeoutSecs = -1)
    {
        CancellationTokenSource cts = new();
        if (timeoutSecs > 0)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSecs));
        }

        return app.WaitForAllTextAsync(logTexts, resourceName, cts.Token);
    }
}

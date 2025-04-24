// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.Tests.Utils;
using Aspire.TestUtilities;
using SamplesIntegrationTests;
using SamplesIntegrationTests.Infrastructure;
using Xunit;

namespace Aspire.Playground.Tests;

[RequiresDocker]
public class ProjectSpecificTests(ITestOutputHelper _testOutput)
{
    [Fact]
    public async Task WithDockerfileTest()
    {
        var appHost = await DistributedApplicationTestFactory.CreateAsync(typeof(Projects.WithDockerfile_AppHost), _testOutput);
        await using var app = await appHost.BuildAsync(TestContext.Current.CancellationToken);

        await app.StartAsync(TestContext.Current.CancellationToken);
        await app.WaitForResources(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken);

        await app.WaitForTextAsync($"I'm Batman. - Batman", cancellationToken: TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromMinutes(3), TestContext.Current.CancellationToken);

        app.EnsureNoErrorsLogged();
        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/6867")]
    public async Task KafkaTest()
    {
        var appHost = await DistributedApplicationTestFactory.CreateAsync(typeof(Projects.KafkaBasic_AppHost), _testOutput);
        await using var app = await appHost.BuildAsync(TestContext.Current.CancellationToken);

        await app.StartAsync(TestContext.Current.CancellationToken);
        await app.WaitForResources(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken);

        // Wait for the producer to start sending messages
        await app.WaitForTextAsync("Hello, World!", cancellationToken: TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);

        // Wait for the consumer to receive some messages
        await WaitForAllTextAsync(app,
            [
                "Hello, World! 343",
                "Received 1000 messages."
            ],
            timeoutSecs: 120);

        app.EnsureNoErrorsLogged();
        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequiresDocker]
    [RequiresTools(["func"])]
    public async Task AzureFunctionsTest()
    {
        var appHost = await DistributedApplicationTestFactory.CreateAsync(typeof(Projects.AzureFunctionsEndToEnd_AppHost), _testOutput);
        await using var app = await appHost.BuildAsync(TestContext.Current.CancellationToken);

        await app.StartAsync(TestContext.Current.CancellationToken);
        await app.WaitForResources(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken);

        // Wait for the 'Job host started' message as an indication
        // that the Functions host has initialized correctly
        await WaitForAllTextAsync(app,
            [
                "Worker process started and initialized."
            ],
            resourceName: "funcapp",
            timeoutSecs: 160);

        // Assert that HTTP triggers work correctly
        await AppHostTests.CreateHttpClientWithResilience(app, "funcapp").GetAsync("/api/injected-resources", TestContext.Current.CancellationToken);
        await WaitForAllTextAsync(app,
            [
                "Executed 'Functions.injected-resources'"
            ],
            resourceName: "funcapp",
            timeoutSecs: 160);

        using var apiServiceClient = AppHostTests.CreateHttpClientWithResilience(app, "apiservice");
        // Assert that Azure Storage Queue triggers work correctly
        await apiServiceClient.GetAsync("/publish/asq", TestContext.Current.CancellationToken);
        await WaitForAllTextAsync(app,
            [
                "Executed 'Functions.MyAzureQueueTrigger'"
            ],
            resourceName: "funcapp",
            timeoutSecs: 160);

        // Assert that Azure Storage Blob triggers work correctly
        await apiServiceClient.GetAsync("/publish/blob", TestContext.Current.CancellationToken);
        await WaitForAllTextAsync(app,
            [
                "Executed 'Functions.MyAzureBlobTrigger'"
            ],
            resourceName: "funcapp",
            timeoutSecs: 160);

        // Assert that EventHubs triggers work correctly
        await apiServiceClient.GetAsync("/publish/eventhubs", TestContext.Current.CancellationToken);
        await WaitForAllTextAsync(app,
            [
                "Executed 'Functions.MyEventHubTrigger'"
            ],
            resourceName: "funcapp",
            timeoutSecs: 160);

#if !SKIP_UNSTABLE_EMULATORS // https://github.com/dotnet/aspire/issues/7066
        // Assert that ServiceBus triggers work correctly
        await apiServiceClient.GetAsync("/publish/asb", TestContext.Current.CancellationToken);
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
        await app.StopAsync(TestContext.Current.CancellationToken);
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.Tests.Utils;
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

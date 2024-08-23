// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.Tests.Utils;
using SamplesIntegrationTests;
using SamplesIntegrationTests.Infrastructure;
using Xunit;
using Xunit.Sdk;
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

        await WaitForAllTextAsync(app,
            [
                "Hello, World! 343",
                "Received 1000 messages."
            ],
            timeoutSecs: 120);

        app.EnsureNoErrorsLogged();
        await app.StopAsync();
    }

    internal static async Task WaitForAllTextAsync(DistributedApplication app, IEnumerable<string> logTexts, string? resourceName = null, int timeoutSecs = -1)
    {
        var table = logTexts.ToList();
        try
        {
            var task = app.WaitForTextAsync((log) =>
            {
                foreach (var text in table)
                {
                    if (log.Contains(text))
                    {
                        table.Remove(text);
                        break;
                    }
                }

                return table.Count == 0;
            }, resourceName);

            await (timeoutSecs > 0
                    ? task.WaitAsync(TimeSpan.FromSeconds(timeoutSecs))
                    : task);
        }
        catch (TimeoutException te)
        {
            throw new XunitException($"The following messages were not found: '{string.Join("', '", table)}'", te);
        }
    }
}

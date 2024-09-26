// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Tests;

public class HealthCheckTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task BuildThrowsOnMissingHealthCheckRegistration()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.Services.AddLogging(b => {
            b.AddXunit(testOutputHelper);
            b.AddFakeLogging();
        });

        builder.AddResource(new CustomResource("test"))
               .WithHealthCheck("test_check");
        var app = builder.Build();

        var ex = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
        {
            await app.StartAsync();
        });

        Assert.Equal("A health check registration is missing. Check logs for more details.", ex.Message);

        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        Assert.Contains(
            logs,
            l => l.Message == "The health check 'test_check' is not registered and is required for resource 'test'."
            );
    }

    public class CustomResource(string name) : Resource(name)
    {

    }
}

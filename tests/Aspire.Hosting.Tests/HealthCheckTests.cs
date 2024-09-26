// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Tests;

public class HealthCheckTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task HealthChecksDontRunAfterResourceEntersHealthyState()
    {
        int resourceHitCount = 0;
        int witnessHitCount = 0;
        bool eventRaised = false;

        using var builder = TestDistributedApplicationBuilder.Create();

        // Add check for our resource that will become healthy.
        builder.Services.AddHealthChecks().AddCheck("resource_check", () =>
        {
            resourceHitCount++;
            return HealthCheckResult.Healthy();
        });

        // Add check for our resource that will stay unhealthy.
        builder.Services.AddHealthChecks().AddCheck("witness_check", () =>
        {
            witnessHitCount++;
            return HealthCheckResult.Unhealthy();
        });

        // Apply check to a custom resource.
        var resource = builder.AddResource(new CustomResource("resource"))
                          .WithHealthCheck("resource_check");

        var witness = builder.AddResource(new CustomResource("witness"))
                          .WithHealthCheck("witness_check");

        builder.Eventing.Subscribe<ResourceHealthyEvent>(resource.Resource, (@event, ct) =>
        {
            eventRaised = true;
            return Task.CompletedTask;
        });

        // Start up the app.
        var app = builder.Build();
        await app.StartAsync();

        // Force the resource to a healthy state and the witness to be in an unhealthy state
        // which means that they have both run their health checks at least once.
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        await rns.PublishUpdateAsync(resource.Resource, s => s with { State = KnownResourceStates.Running });
        await rns.PublishUpdateAsync(witness.Resource, s => s with { State = KnownResourceStates.Running });
        await rns.WaitForResourceHealthyAsync(resource.Resource.Name);
        await rns.WaitForResourceAsync(witness.Resource.Name, re => re.Snapshot.HealthStatus == HealthStatus.Unhealthy);

        // First time both witness and resource should be at hit count 1.
        Assert.Equal(1, resourceHitCount);
        Assert.True(witnessHitCount > 0); // To cater for unexpected stall on machine.

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Wait until the witness moves forward one.
        while (true)
        {
            cts.Token.ThrowIfCancellationRequested();

            if (witnessHitCount > 1)
            {
                Assert.Equal(1, resourceHitCount);
                break;
            }
        }

        Assert.True(eventRaised);
    }

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

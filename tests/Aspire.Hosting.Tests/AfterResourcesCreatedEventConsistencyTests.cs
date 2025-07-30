// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Tests;

public class AfterResourcesCreatedEventConsistencyTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task AfterResourcesCreatedEvent_ShouldFireConsistently_WithoutWaitFor()
    {
        var eventFired = false;

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);
        builder.Services.AddHealthChecks().AddCheck("alwaysUnhealthy", () => HealthCheckResult.Unhealthy());

        var one = builder.AddContainer("one", "nginx").WithHealthCheck("alwaysUnhealthy");
        var two = builder.AddContainer("two", "nginx");

        // No WaitFor here - this is the key difference

        builder.Eventing.Subscribe<AfterResourcesCreatedEvent>((evt, ct) =>
        {
            eventFired = true;
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        
        // Start the app but don't wait too long as resources may fail to start due to Docker unavailability
        var cts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.DefaultTimeoutDuration);
        await app.StartAsync(cts.Token);

        // Give some time for the event to fire if it's going to
        await Task.Delay(1000);

        // In the current implementation, the event should fire regardless of Docker availability
        // when there are no WaitFor dependencies
        Assert.True(eventFired, "AfterResourcesCreatedEvent should fire without WaitFor dependencies");

        await app.StopAsync();
    }

    [Fact]
    public async Task AfterResourcesCreatedEvent_ShouldFireConsistently_WithWaitFor()
    {
        var eventFired = false;

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);
        builder.Services.AddHealthChecks().AddCheck("alwaysUnhealthy", () => HealthCheckResult.Unhealthy());

        var one = builder.AddContainer("one", "nginx").WithHealthCheck("alwaysUnhealthy");
        var two = builder.AddContainer("two", "nginx");

        two.WaitFor(one); // This creates the inconsistency

        builder.Eventing.Subscribe<AfterResourcesCreatedEvent>((evt, ct) =>
        {
            eventFired = true;
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        
        // Start the app but don't wait too long as resources may fail to start due to Docker unavailability
        var cts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.DefaultTimeoutDuration);
        await app.StartAsync(cts.Token);

        // Give some time for the event to fire if it's going to
        await Task.Delay(1000);

        // Currently, this fires even when Docker is unavailable, which is inconsistent with the no-WaitFor case
        // The behavior should be consistent in both cases
        Assert.True(eventFired, "AfterResourcesCreatedEvent should fire consistently with WaitFor dependencies");

        await app.StopAsync();
    }

    [Fact]
    public async Task AfterResourcesCreatedEvent_ConsistencyBetweenWaitForAndNonWaitForScenarios()
    {
        var eventFiredWithoutWaitFor = false;
        var eventFiredWithWaitFor = false;

        // Test scenario without WaitFor
        using (var builder1 = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper))
        {
            builder1.Services.AddHealthChecks().AddCheck("alwaysUnhealthy", () => HealthCheckResult.Unhealthy());

            var one1 = builder1.AddContainer("one", "nginx").WithHealthCheck("alwaysUnhealthy");
            var two1 = builder1.AddContainer("two", "nginx");

            builder1.Eventing.Subscribe<AfterResourcesCreatedEvent>((evt, ct) =>
            {
                eventFiredWithoutWaitFor = true;
                return Task.CompletedTask;
            });

            using var app1 = builder1.Build();
            var cts1 = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.DefaultTimeoutDuration);
            await app1.StartAsync(cts1.Token);
            await Task.Delay(1000);
            await app1.StopAsync();
        }

        // Test scenario with WaitFor
        using (var builder2 = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper))
        {
            builder2.Services.AddHealthChecks().AddCheck("alwaysUnhealthy", () => HealthCheckResult.Unhealthy());

            var one2 = builder2.AddContainer("one", "nginx").WithHealthCheck("alwaysUnhealthy");
            var two2 = builder2.AddContainer("two", "nginx");

            two2.WaitFor(one2);

            builder2.Eventing.Subscribe<AfterResourcesCreatedEvent>((evt, ct) =>
            {
                eventFiredWithWaitFor = true;
                return Task.CompletedTask;
            });

            using var app2 = builder2.Build();
            var cts2 = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.DefaultTimeoutDuration);
            await app2.StartAsync(cts2.Token);
            await Task.Delay(1000);
            await app2.StopAsync();
        }

        // Both scenarios should behave consistently
        Assert.Equal(eventFiredWithoutWaitFor, eventFiredWithWaitFor);
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class AfterResourcesCreatedEventTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task AfterResourcesCreatedEventFiresConsistentlyWithoutWaitFor()
    {
        var eventFired = new TaskCompletionSource<bool>();

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);
        
        var one = builder.AddContainer("one", "nginx");
        var two = builder.AddContainer("two", "nginx");

#pragma warning disable CS0618 // Type or member is obsolete
        builder.Eventing.Subscribe<Aspire.Hosting.ApplicationModel.AfterResourcesCreatedEvent>((evt, ct) =>
        {
            eventFired.TrySetResult(true);
            return Task.CompletedTask;
        });
#pragma warning restore CS0618 // Type or member is obsolete

        using var app = builder.Build();
        
        // Start the app asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await app.StartAsync();
            }
            catch
            {
                // Ignore startup errors for this test - we only care about event firing
            }
        });
        
        // Wait for the event to fire or timeout after 10 seconds
        var eventFiredOrTimeout = await Task.WhenAny(eventFired.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        
        Assert.True(eventFiredOrTimeout == eventFired.Task, "AfterResourcesCreatedEvent should fire within 10 seconds without WaitFor");
        Assert.True(await eventFired.Task, "AfterResourcesCreatedEvent should have fired");
        
        // Clean up
        try
        {
            await app.StopAsync();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
    
    [Fact]
    public async Task AfterResourcesCreatedEventFiresConsistentlyWithWaitFor()
    {
        var eventFired = new TaskCompletionSource<bool>();

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);
        
        // Add a health check that will always be unhealthy
        builder.Services.AddHealthChecks().AddCheck("alwaysUnhealthy", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy());

        var one = builder.AddContainer("one", "nginx").WithHealthCheck("alwaysUnhealthy");
        var two = builder.AddContainer("two", "nginx");

        // Use WaitFor which used to block the event
        two.WaitFor(one);

#pragma warning disable CS0618 // Type or member is obsolete
        builder.Eventing.Subscribe<Aspire.Hosting.ApplicationModel.AfterResourcesCreatedEvent>((evt, ct) =>
        {
            eventFired.TrySetResult(true);
            return Task.CompletedTask;
        });
#pragma warning restore CS0618 // Type or member is obsolete

        using var app = builder.Build();
        
        // Start the app asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await app.StartAsync();
            }
            catch
            {
                // Ignore startup errors for this test - we only care about event firing
            }
        });
        
        // Wait for the event to fire or timeout after 10 seconds
        var eventFiredOrTimeout = await Task.WhenAny(eventFired.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        
        Assert.True(eventFiredOrTimeout == eventFired.Task, "AfterResourcesCreatedEvent should fire within 10 seconds even with WaitFor");
        Assert.True(await eventFired.Task, "AfterResourcesCreatedEvent should have fired");
        
        // Clean up
        try
        {
            await app.StopAsync();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
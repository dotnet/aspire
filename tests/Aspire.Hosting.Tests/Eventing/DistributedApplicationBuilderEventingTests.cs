// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREEVENTING001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.Eventing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Eventing;

public class DistributedApplicationBuilderEventingTests
{
    [Fact]
    public void CanResolveIDistributedApplicationEventingFromDI()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var app = builder.Build();
        var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();
        Assert.Equal(builder.Eventing, eventing);
    }

    [Fact]
    public async Task LifeycleHookAnalogousEventsFire()
    {
        var beforeStartEventFired = new ManualResetEventSlim();
        var afterEndpointsAllocatedEventFired = new ManualResetEventSlim();
        var afterResourcesCreatedEventFired = new ManualResetEventSlim();

        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Eventing.Subscribe<BeforeStartEvent>((e, ct) =>
        {
            beforeStartEventFired.Set();
            return Task.CompletedTask;
        });
        builder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((e, ct) =>
        {
            afterEndpointsAllocatedEventFired.Set();
            return Task.CompletedTask;
        });
        builder.Eventing.Subscribe<AfterResourcesCreatedEvent>((e, ct) =>
        {
            afterResourcesCreatedEventFired.Set();
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        await app.StartAsync();

        var allFired = ManualResetEvent.WaitAll(
            [beforeStartEventFired.WaitHandle, afterEndpointsAllocatedEventFired.WaitHandle, afterResourcesCreatedEventFired.WaitHandle],
            TimeSpan.FromSeconds(10)
            );

        Assert.True(allFired);
        await app.StopAsync();
    }
}

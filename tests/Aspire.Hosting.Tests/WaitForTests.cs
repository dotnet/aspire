// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WaitForTests
{
    [Fact]
    [RequiresDocker]
    public async Task EnsureDependentResourceMovesIntoWaitingState()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var dependency = builder.AddResource(new CustomResource("test"));
        var redis = builder.AddRedis("redis")
                           .WithReference(dependency)
                           .WaitFor(dependency);

        using var app = builder.Build();

        // StartAsync will currently block until the dependency resource moves
        // into a Running state, so rather than awaiting it we'll hold onto the
        // task so we can inspect the state of the Redis resource which should
        // be in a waiting state if everything is working correctly.
        var startTask = app.StartAsync();

        // We don't want to wait forever for Redis to move into a waiting state,
        // it should be super quick, but we'll allow 10 seconds just in case the
        // CI machine is chugging.
        var waitingStateCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        await rns.WaitForResourceAsync(redis.Resource.Name, "Waiting", waitingStateCts.Token);

        // Now that we know we successfully entered the Waiting state, we can swap
        // the dependency into a running state which will unblock startup and
        // we can continue executing.
        await rns.PublishUpdateAsync(dependency.Resource, s => s with { State = KnownResourceStates.Running });

        await startTask;

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task EnsureDependentResourceMovesIntoWaitingStateUntilDependencyMovesToFinishedState()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var dependency = builder.AddResource(new CustomResource("test"));
        var redis = builder.AddRedis("redis")
                           .WithReference(dependency)
                           .WaitForCompletion(dependency);

        using var app = builder.Build();

        // StartAsync will currently block until the dependency resource moves
        // into a Finished state, so rather than awaiting it we'll hold onto the
        // task so we can inspect the state of the Redis resource which should
        // be in a waiting state if everything is working correctly.
        var startTask = app.StartAsync();

        // We don't want to wait forever for Redis to move into a waiting state,
        // it should be super quick, but we'll allow 10 seconds just in case the
        // CI machine is chugging.
        var waitingStateCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        await rns.WaitForResourceAsync(redis.Resource.Name, "Waiting", waitingStateCts.Token);

        // Now that we know we successfully entered the Waiting state, we can swap
        // the dependency into a running state which will unblock startup and
        // we can continue executing.
        await rns.PublishUpdateAsync(dependency.Resource, s => s with { State = KnownResourceStates.Finished });

        // This time we want to wait for Redis to move into a Running state to verify that
        // it successfully started after we moved the dependency resource into the Finished, but
        // we need to give it more time since we have to download the image in CI.
        var runningStateCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await rns.WaitForResourceAsync(redis.Resource.Name, KnownResourceStates.Running, runningStateCts.Token);

        await startTask;

        await app.StopAsync();
    }

    private sealed class CustomResource(string name) : Resource(name), IResourceWithConnectionString
    {
        public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"foo");
    }
}

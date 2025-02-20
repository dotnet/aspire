// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Dashboard.Model;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Tests;

public class WaitForTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task ResourceThatFailsToStartDueToExceptionDoesNotCauseStartAsyncToThrow()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);
        var throwingResource = builder.AddContainer("throwingresource", "doesnotmatter")
                              .WithEnvironment(ctx => throw new InvalidOperationException("BOOM!"));
        var dependingContainerResource = builder.AddContainer("dependingcontainerresource", "doesnotmatter")
                                       .WaitFor(throwingResource);
        var dependingExecutableResource = builder.AddExecutable("dependingexecutableresource", "doesnotmatter", "alsodoesntmatter")
                                       .WaitFor(throwingResource);

        var abortCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        using var app = builder.Build();
        await app.StartAsync(abortCts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(throwingResource.Resource.Name, KnownResourceStates.FailedToStart, abortCts.Token);
        await app.ResourceNotifications.WaitForResourceAsync(dependingContainerResource.Resource.Name, KnownResourceStates.FailedToStart, abortCts.Token);
        await app.ResourceNotifications.WaitForResourceAsync(dependingExecutableResource.Resource.Name, KnownResourceStates.FailedToStart, abortCts.Token);

        await app.StopAsync(abortCts.Token);
    }

    [Fact]
    public void ResourceCannotWaitForItself()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddResource(new CustomResource("test"));

        var waitForEx = Assert.Throws<DistributedApplicationException>(() =>
        {
            resource.WaitFor(resource);
        });

        Assert.Equal("The 'test' resource cannot wait for itself.", waitForEx.Message);

        var waitForCompletionEx = Assert.Throws<DistributedApplicationException>(() =>
        {
            resource.WaitForCompletion(resource);
        });

        Assert.Equal("The 'test' resource cannot wait for itself.", waitForCompletionEx.Message);
    }

    [Fact]
    public void ResourceCannotWaitForItsParent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var parentResourceBuilder = builder.AddResource(new CustomResource("parent"));
        var childResourceBuilder = builder.AddResource(new CustomChildResource("child", parentResourceBuilder.Resource));

        var waitForEx = Assert.Throws<DistributedApplicationException>(() =>
        {
            childResourceBuilder.WaitFor(parentResourceBuilder);
        });

        Assert.Equal("The 'child' resource cannot wait for its parent 'parent'.", waitForEx.Message);

        var waitForCompletionEx = Assert.Throws<DistributedApplicationException>(() =>
        {
            childResourceBuilder.WaitForCompletion(parentResourceBuilder);
        });

        Assert.Equal("The 'child' resource cannot wait for its parent 'parent'.", waitForCompletionEx.Message);
    }

    [Fact]
    [RequiresDocker]
    public async Task WaitingForParameterResourceCompletesImmediately()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        builder.Configuration["ConnectionStrings:cs"] = "cs-value";

        // This test waits for a parameter, a connection string, and a custom resource.
        // The only thing being waited on should be the custom resource.

        var dependency = builder.AddResource(new CustomResource("test"));
        var cs = builder.AddConnectionString("cs");
        var param = builder.AddParameter("param", "value");
        var nginx = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                           .WaitFor(cs)
                           .WaitFor(param)
                           .WaitFor(dependency);

        using var app = builder.Build();

        using var startupCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        var startTask = app.StartAsync(startupCts.Token);

        using var waitingStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Waiting, waitingStateCts.Token);

        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, s => s with
        {
            State = KnownResourceStates.Running
        });

        // Notice we don't need to move the parameter or connection string to a running state
        await startTask;

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task EnsureDependentResourceMovesIntoWaitingState()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

        var dependency = builder.AddResource(new CustomResource("test"));
        var nginx = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                           .WithReference(dependency)
                           .WaitFor(dependency);

        using var app = builder.Build();

        // StartAsync will currently block until the dependency resource moves
        // into a Running state, so rather than awaiting it we'll hold onto the
        // task so we can inspect the state of the Nginx resource which should
        // be in a waiting state if everything is working correctly.
        var startupCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        var startTask = app.StartAsync(startupCts.Token);

        // We don't want to wait forever for Nginx to move into a waiting state,
        // it should be super quick, but we'll allow a long timeout just in case the
        // CI machine is chugging (also useful when collecting code coverage).
        var waitingStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Waiting, waitingStateCts.Token);

        // Now that we know we successfully entered the Waiting state, we can swap
        // the dependency into a running state which will unblock startup and
        // we can continue executing.
        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, s => s with
        {
            State = KnownResourceStates.Running
        });

        await startTask;

        await app.StopAsync();
    }

    // Add a test that verifies the wait for behavior when the dependency is in varying states
    // and the dependent resource is waiting for the dependency.
    // Use a theory to test the different states and expected behavior.

    [Theory]
    [InlineData(nameof(KnownResourceStates.Exited))]
    [InlineData(nameof(KnownResourceStates.FailedToStart))]
    [InlineData(nameof(KnownResourceStates.RuntimeUnhealthy))]
    [InlineData(nameof(KnownResourceStates.Finished))]
    [RequiresDocker]
    public async Task WaitForBehaviorStopOnResourceUnavailable(string status)
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

        var dependency = builder.AddResource(new CustomResource("test"));
        var nginx = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                           .WithReference(dependency)
                           .WaitFor(dependency, WaitBehavior.Throw);

        using var app = builder.Build();

        var startupCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        var startTask = app.StartAsync(startupCts.Token);

        var waitingStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Waiting, waitingStateCts.Token);

        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, s => s with
        {
            State = status
        });

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.FailedToStart, waitingStateCts.Token);

        await startTask;
    }

   [Fact]
   public async Task WhenWaitBehaviorIsStopOnResourceUnavailableWaitForResourceHealthyAsyncShouldThrowWhenResourceFailsToStart()
   {
      using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

      var failToStart = builder.AddExecutable("failToStart", "does-not-exist", ".");
      var dependency = builder.AddContainer("redis", "redis");

      dependency.WaitFor(failToStart, WaitBehavior.Throw);

      using var app = builder.Build();
      await app.StartAsync();

      var ex = await Assert.ThrowsAsync<DistributedApplicationException>(async () => {
        await app.ResourceNotifications.WaitForResourceHealthyAsync(
            dependency.Resource.Name,
            WaitBehavior.Throw
            ).WaitAsync(TimeSpan.FromSeconds(15));
      });

      Assert.Equal("Stopped waiting for resource 'redis' to become healthy because it failed to start.", ex.Message);
   }

   [Fact]
   public async Task WhenWaitBehaviorIsWaitOnResourceUnavailableWaitForResourceHealthyAsyncShouldThrowWhenResourceFailsToStart()
   {
      using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

      var failToStart = builder.AddExecutable("failToStart", "does-not-exist", ".");
      var dependency = builder.AddContainer("redis", "redis");

      dependency.WaitFor(failToStart, WaitBehavior.Throw);

      using var app = builder.Build();
      await app.StartAsync();

      var ex = await Assert.ThrowsAsync<TimeoutException>(async () => {
        await app.ResourceNotifications.WaitForResourceHealthyAsync(
            dependency.Resource.Name,
            WaitBehavior.Wait
            ).WaitAsync(TimeSpan.FromSeconds(15));
      });

      Assert.Equal("The operation has timed out.", ex.Message);
   }

    [Theory]
    [InlineData(nameof(KnownResourceStates.Exited))]
    [InlineData(nameof(KnownResourceStates.FailedToStart))]
    [InlineData(nameof(KnownResourceStates.RuntimeUnhealthy))]
    [InlineData(nameof(KnownResourceStates.Finished))]
    [RequiresDocker]
    public async Task WaitForBehaviorStopOnDependencyIsDefaultWithNoDashboardFailure(string status)
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

        var dependency = builder.AddResource(new CustomResource("test"));
        var nginx = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                           .WithReference(dependency)
                           .WaitFor(dependency);

        using var app = builder.Build();

        var startupCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        var startTask = app.StartAsync(startupCts.Token);

        var waitingStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Waiting, waitingStateCts.Token);

        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, s => s with
        {
            State = status
        });

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.FailedToStart, waitingStateCts.Token);

        await startTask;
    }

    [Theory]
    [InlineData(nameof(KnownResourceStates.Exited))]
    [InlineData(nameof(KnownResourceStates.FailedToStart))]
    [InlineData(nameof(KnownResourceStates.RuntimeUnhealthy))]
    [InlineData(nameof(KnownResourceStates.Finished))]
    [RequiresDocker]
    public async Task WaitForBehaviorWaitOnResourceUnavailable(string status)
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

        var dependency = builder.AddResource(new CustomResource("test"));
        var nginx = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                           .WithReference(dependency)
                           .WaitFor(dependency, WaitBehavior.Wait);

        using var app = builder.Build();

        var startupCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        var startTask = app.StartAsync(startupCts.Token);

        var waitingStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Waiting, waitingStateCts.Token);

        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, s => s with
        {
            State = status
        });

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Waiting, waitingStateCts.Token);

        // Fake a restart of the dependency
        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, s => s with
        {
            State = KnownResourceStates.Running
        });

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Running, waitingStateCts.Token);

        await startTask;
    }

    [Theory]
    [InlineData(nameof(KnownResourceStates.Exited))]
    [InlineData(nameof(KnownResourceStates.FailedToStart))]
    [InlineData(nameof(KnownResourceStates.RuntimeUnhealthy))]
    [InlineData(nameof(KnownResourceStates.Finished))]
    [RequiresDocker]
    public async Task WaitForBehaviorWaitOnResourceUnavailableViaOptions(string status)
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

        builder.Services.Configure<ResourceNotificationServiceOptions>(o =>
        {
            o.DefaultWaitBehavior = WaitBehavior.Wait;
        });

        var dependency = builder.AddResource(new CustomResource("test"));
        var nginx = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                           .WithReference(dependency)
                           .WaitFor(dependency);

        using var app = builder.Build();

        var startupCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        var startTask = app.StartAsync(startupCts.Token);

        var waitingStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Waiting, waitingStateCts.Token);

        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, s => s with
        {
            State = status
        });

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Waiting, waitingStateCts.Token);

        // Fake a restart of the dependency
        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, s => s with
        {
            State = KnownResourceStates.Running
        });

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Running, waitingStateCts.Token);

        await startTask;
    }

    [Fact]
    [RequiresDocker]
    public async Task WaitForCompletionWaitsForTerminalStateOfDependencyResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

        var dependency = builder.AddResource(new CustomResource("test"));
        var nginx = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                           .WithReference(dependency)
                           .WaitForCompletion(dependency);

        using var app = builder.Build();

        // StartAsync will currently block until the dependency resource moves
        // into a Finished state, so rather than awaiting it we'll hold onto the
        // task so we can inspect the state of the Nginx resource which should
        // be in a waiting state if everything is working correctly.
        var startupCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        var startTask = app.StartAsync(startupCts.Token);

        // We don't want to wait forever for Nginx to move into a waiting state,
        // it should be super quick, but we'll allow 60 seconds just in case the
        // CI machine is chugging (also useful when collecting code coverage).
        var waitingStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Waiting, waitingStateCts.Token);

        // Now that we know we successfully entered the Waiting state, we can swap
        // the dependency into a running state which will unblock startup and
        // we can continue executing.
        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, s => s with
        {
            State = KnownResourceStates.Finished,
            ExitCode = 0
        });

        // This time we want to wait for Nginx to move into a Running state to verify that
        // it successfully started after we moved the dependency resource into the Finished, but
        // we need to give it more time since we have to download the image in CI.
        var runningStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Running, runningStateCts.Token);

        await startTask;

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task WaitForThrowsIfResourceMovesToTerminalStateBeforeRunning()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

        var dependency = builder.AddResource(new CustomResource("test"));
        var nginx = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                           .WithReference(dependency)
                           .WaitFor(dependency);

        using var app = builder.Build();

        // StartAsync will currently block until the dependency resource moves
        // into a Finished state, so rather than awaiting it we'll hold onto the
        // task so we can inspect the state of the Nginx resource which should
        // be in a waiting state if everything is working correctly.
        var startupCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        var startTask = app.StartAsync(startupCts.Token);

        // We don't want to wait forever for Nginx to move into a waiting state,
        // it should be super quick, but we'll allow 60 seconds just in case the
        // CI machine is chugging (also useful when collecting code coverage).
        var waitingStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, "Waiting", waitingStateCts.Token);

        // Now that we know we successfully entered the Waiting state, we can swap
        // the dependency into a running state which will unblock startup and
        // we can continue executing.
        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, s => s with
        {
            State = KnownResourceStates.Finished,
            ExitCode = 0
        });

        // This time we want to wait for Nginx to move into a Running state to verify that
        // it successfully started after we moved the dependency resource into the Finished, but
        // we need to give it more time since we have to download the image in CI.
        var runningStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.FailedToStart, runningStateCts.Token);

        await startTask;

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task WaitForObservedResultOfResourceReadyEvent()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

        builder.Services.AddLogging(b =>
        {
            b.AddFakeLogging();
        });

        var resourceReadyTcs = new TaskCompletionSource();
        var dependency = builder.AddResource(new CustomResource("test"));
        var nginx = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                           .WithReference(dependency)
                           .WaitFor(dependency);

        builder.Eventing.Subscribe<ResourceReadyEvent>(dependency.Resource, (e, ct) => resourceReadyTcs.Task);

        using var app = builder.Build();

        // StartAsync will currently block until the dependency resource moves
        // into a Finished state, so rather than awaiting it we'll hold onto the
        // task so we can inspect the state of the Nginx resource which should
        // be in a waiting state if everything is working correctly.
        var startupCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        var startTask = app.StartAsync(startupCts.Token);

        // We don't want to wait forever for Nginx to move into a waiting state,
        // it should be super quick, but we'll allow 60 seconds just in case the
        // CI machine is chugging (also useful when collecting code coverage).
        var waitingStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, "Waiting", waitingStateCts.Token);

        // Now that we know we successfully entered the Waiting state, we can swap
        // the dependency into a running state which will unblock startup and
        // we can continue executing.
        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, s => s with
        {
            State = KnownResourceStates.Running
        });

        resourceReadyTcs.SetException(new InvalidOperationException("The resource ready event failed!"));

        // This time we want to wait for Nginx to move into a Running state to verify that
        // it successfully started after we moved the dependency resource into the Finished, but
        // we need to give it more time since we have to download the image in CI.
        var runningStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.FailedToStart, runningStateCts.Token);

        await startTask;

        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        // Just looking for a common message in Docker build output.
        Assert.Contains(logs, log => log.Message.Contains("The resource ready event failed!"));

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task EnsureDependencyResourceThatReturnsNonMatchingExitCodeResultsInDependentResourceFailingToStart()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

        var dependency = builder.AddResource(new CustomResource("test"));
        var nginx = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                           .WithReference(dependency)
                           .WaitForCompletion(dependency, exitCode: 2);

        using var app = builder.Build();

        // StartAsync will currently block until the dependency resource moves
        // into a Finished state, so rather than awaiting it we'll hold onto the
        // task so we can inspect the state of the Nginx resource which should
        // be in a waiting state if everything is working correctly.
        var startupCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        var startTask = app.StartAsync(startupCts.Token);

        // We don't want to wait forever for Nginx to move into a waiting state,
        // it should be super quick, but we'll allow 60 seconds just in case the
        // CI machine is chugging (also useful when collecting code coverage).
        var waitingStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Waiting, waitingStateCts.Token);

        // Now that we know we successfully entered the Waiting state, we can swap
        // the dependency into a finished state which will unblock startup and
        // we can continue executing.
        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, s => s with
        {
            State = KnownResourceStates.Finished,
            ExitCode = 3 // Exit code does not match expected exit code above intentionally.
        });

        // This time we want to wait for Nginx to move into a FailedToStart state to verify that
        // it didn't start if the dependency resource didn't finish with the correct exit code.
        var runningStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.FailedToStart, runningStateCts.Token);

        await startTask;

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task DependencyWithGreaterThan1ReplicaAnnotationWaitsForAll()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

        var dependency = builder.AddResource(new CustomResource("test"))
                                .WithAnnotation(new ReplicaAnnotation(2))
                                .WithAnnotation(new DcpInstancesAnnotation([
                                    new("test0", "", 0), new("test1", "", 1)
                                ]));

        var nginx = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                           .WithReference(dependency)
                           .WaitFor(dependency);

        using var app = builder.Build();

        // StartAsync will currently block until the dependency resource moves
        // into a Finished state, so rather than awaiting it we'll hold onto the
        // task so we can inspect the state of the Nginx resource which should
        // be in a waiting state if everything is working correctly.
        var startupCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        var startTask = app.StartAsync(startupCts.Token);

        // We don't want to wait forever for Nginx to move into a waiting state,
        // it should be super quick, but we'll allow 60 seconds just in case the
        // CI machine is chugging (also useful when collecting code coverage).
        var waitingStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Waiting, waitingStateCts.Token);

        // Publish the first replica as finished
        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, "test0", s => s with
        {
            State = KnownResourceStates.Running,
        });

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Waiting, waitingStateCts.Token);

        // Publish the second replica as finished
        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, "test1", s => s with
        {
            State = KnownResourceStates.Running,
        });

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Running, waitingStateCts.Token);

        await startTask;

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task DependencyWithGreaterThan1ReplicaAnnotationWaitsForAllToComplete()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

        var dependency = builder.AddResource(new CustomResource("test"))
                                .WithAnnotation(new ReplicaAnnotation(2))
                                .WithAnnotation(new DcpInstancesAnnotation([
                                    new("test0", "", 0), new("test1", "", 1)
                                ]));

        var nginx = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                           .WithReference(dependency)
                           .WaitForCompletion(dependency);

        using var app = builder.Build();

        // StartAsync will currently block until the dependency resource moves
        // into a Finished state, so rather than awaiting it we'll hold onto the
        // task so we can inspect the state of the Nginx resource which should
        // be in a waiting state if everything is working correctly.
        var startupCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        var startTask = app.StartAsync(startupCts.Token);

        // We don't want to wait forever for Nginx to move into a waiting state,
        // it should be super quick, but we'll allow 60 seconds just in case the
        // CI machine is chugging (also useful when collecting code coverage).
        var waitingStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Waiting, waitingStateCts.Token);

        // Publish the first replica as finished
        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, "test0", s => s with
        {
            State = KnownResourceStates.Finished,
        });

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Waiting, waitingStateCts.Token);

        // Publish the second replica as finished
        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, "test1", s => s with
        {
            State = KnownResourceStates.Finished,
        });

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Running, waitingStateCts.Token);

        await startTask;

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task WaitForCompletionSucceedsIfDependentResourceEntersTerminalStateWithoutAnExitCode()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

        var dependency = builder.AddResource(new CustomResource("test"));

        var nginx = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                           .WithReference(dependency)
                           .WaitForCompletion(dependency);

        using var app = builder.Build();

        // StartAsync will currently block until the dependency resource moves
        // into a Finished state, so rather than awaiting it we'll hold onto the
        // task so we can inspect the state of the Nginx resource which should
        // be in a waiting state if everything is working correctly.
        var startupCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        var startTask = app.StartAsync(startupCts.Token);

        // We don't want to wait forever for Nginx to move into a waiting state,
        // it should be super quick, but we'll allow 60 seconds just in case the
        // CI machine is chugging (also useful when collecting code coverage).
        var waitingStateCts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Waiting, waitingStateCts.Token);

        // Now that we know we successfully entered the Waiting state, we can end the dependency
        await app.ResourceNotifications.PublishUpdateAsync(dependency.Resource, s => s with
        {
            State = KnownResourceStates.Finished
        });

        await app.ResourceNotifications.WaitForResourceAsync(nginx.Resource.Name, KnownResourceStates.Running, waitingStateCts.Token);

        await startTask;

        await app.StopAsync();
    }

    [Fact]
    public void WaitForOnChildResourceAddsWaitAnnotationPointingToParent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var parentResource = builder.AddResource(new CustomResource("parent"));
        var childResource = builder.AddResource(new CustomChildResource("child", parentResource.Resource));
        var containerResource = builder.AddContainer("container", "image", "tag")
                                       .WaitFor(childResource);

        Assert.True(containerResource.Resource.TryGetAnnotationsOfType<WaitAnnotation>(out var waitAnnotations));
        Assert.Collection(
            waitAnnotations,
            a => Assert.Equal(a.Resource, parentResource.Resource),
            a => Assert.Equal(a.Resource, childResource.Resource)
            );

        Assert.True(containerResource.Resource.TryGetAnnotationsOfType<ResourceRelationshipAnnotation>(out var relationshipAnnotations));
        var relationshipAnnotation = Assert.Single(relationshipAnnotations);

        Assert.Equal(childResource.Resource, relationshipAnnotation.Resource);
        Assert.Equal(KnownRelationshipTypes.WaitFor, relationshipAnnotation.Type);
    }

    private sealed class CustomChildResource(string name, CustomResource parent) : Resource(name), IResourceWithParent<CustomResource>, IResourceWithWaitSupport
    {
        public CustomResource Parent => parent;
    }

    private sealed class CustomResource(string name) : Resource(name), IResourceWithConnectionString, IResourceWithWaitSupport
    {
        public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"foo");
    }
}

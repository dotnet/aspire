// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

public class OperationModesTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task VerifyBackwardsCompatibleRunModeInvocation()
    {
        // The purpose of this test is to verify that the apphost executable will continue
        // to enter run mode if executed without any arguments.

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        
        var tcs = new TaskCompletionSource<DistributedApplicationExecutionContext>();
        builder.Eventing.Subscribe<AfterResourcesCreatedEvent>((e, ct) => {
            var context = e.Services.GetRequiredService<DistributedApplicationExecutionContext>();
            tcs.SetResult(context);
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        
        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        var context = await tcs.Task.WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        await app.StopAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        Assert.Equal(DistributedApplicationOperation.Run, context.Operation);
        Assert.True(context.IsRunMode);
    }

    [Fact]
    public async Task VerifyExplicitRunModeInvocation()
    {
        // The purpose of this test is to verify that the apphost executable will enter
        // run mode if executed with the "--operation run" argument.

        using var builder = TestDistributedApplicationBuilder
            .Create(["--operation", "run"])
            .WithTestAndResourceLogging(outputHelper);
        
        var tcs = new TaskCompletionSource<DistributedApplicationExecutionContext>();
        builder.Eventing.Subscribe<AfterResourcesCreatedEvent>((e, ct) => {
            var context = e.Services.GetRequiredService<DistributedApplicationExecutionContext>();
            tcs.SetResult(context);
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        
        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        var context = await tcs.Task.WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        await app.StopAsync().WaitAsync(TestConstants.LongTimeoutTimeSpan);

        Assert.Equal(DistributedApplicationOperation.Run, context.Operation);
        Assert.True(context.IsRunMode);
    }

    [Fact]
    public async Task VerifyExplicitRunModeWithPublisherInvocation()
    {
        // The purpose of this test is to verify that the apphost executable will enter
        // run mode if executed with the "--operation run" argument.

        using var builder = TestDistributedApplicationBuilder
            .Create(["--operation", "run", "--publisher", "manifest"])
            .WithTestAndResourceLogging(outputHelper);
        
        var tcs = new TaskCompletionSource<DistributedApplicationExecutionContext>();
        builder.Eventing.Subscribe<AfterResourcesCreatedEvent>((e, ct) => {
            var context = e.Services.GetRequiredService<DistributedApplicationExecutionContext>();
            tcs.SetResult(context);
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        
        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        var context = await tcs.Task.WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        await app.StopAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        Assert.Equal(DistributedApplicationOperation.Run, context.Operation);
        Assert.True(context.IsRunMode);
    }

    [Fact]
    public async Task VerifyBackwardsCompatiblePublishModeInvocation()
    {
        // The purpose of this test is to verify that the apphost executable will continue
        // to enter publish mode if the --publisher argument is specified.

        using var builder = TestDistributedApplicationBuilder
            .Create(["--publisher", "manifest", "--output-path", "test-output-path"])
            .WithTestAndResourceLogging(outputHelper);

        // TOOD: This won't work because this event does not fire in publish mode. We need
        //       another way to get at this internal state.
        var tcs = new TaskCompletionSource<DistributedApplicationExecutionContext>();
        builder.Eventing.Subscribe<BeforeStartEvent>((e, ct) => {
            var context = e.Services.GetRequiredService<DistributedApplicationExecutionContext>();
            tcs.SetResult(context);
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        
        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        var context = await tcs.Task.WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        await app.StopAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        Assert.Equal(DistributedApplicationOperation.Publish, context.Operation);
        Assert.True(context.IsPublishMode);
    }

    [Fact]
    public void VerifyExplicitPublishModeInvocation()
    {
        // The purpose of this test is to verify that the apphost executable will continue
        // to enter publish mode if the --publisher argument is specified.

        using var builder = TestDistributedApplicationBuilder
            .Create(["--operation", "publish", "--publisher", "manifest", "--output-path", "test-output-path"])
            .WithTestAndResourceLogging(outputHelper);
        Assert.Equal(DistributedApplicationOperation.Publish, builder.ExecutionContext.Operation);
    }

    [Fact]
    public async Task VerifyExplicitInspectModeInvocation()
    {
        // The purpose of this test is to verify that the apphost executable will enter
        // inspect mode if executed with the "--operation inspect" argument.

        using var builder = TestDistributedApplicationBuilder
            .Create(["--operation", "inspect"])
            .WithTestAndResourceLogging(outputHelper);
        
        var tcs = new TaskCompletionSource<DistributedApplicationExecutionContext>();
        builder.Eventing.Subscribe<AfterResourcesCreatedEvent>((e, ct) => {
            var context = e.Services.GetRequiredService<DistributedApplicationExecutionContext>();
            tcs.SetResult(context);
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        
        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        var context = await tcs.Task.WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        await app.StopAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        Assert.Equal(DistributedApplicationOperation.Inspect, context.Operation);
        Assert.True(context.IsInspectMode);
        Assert.False(context.IsRunMode);
        Assert.False(context.IsPublishMode);
    }

    [Fact]
    public void VerifyInspectModeDoesNotRegisterDcp()
    {
        // Verify that DCP services are not registered in inspect mode
        using var builder = TestDistributedApplicationBuilder
            .Create(["--operation", "inspect"])
            .WithTestAndResourceLogging(outputHelper);

        Assert.Equal(DistributedApplicationOperation.Inspect, builder.ExecutionContext.Operation);
        
        using var app = builder.Build();
        
        // Verify DCP-related services are not registered
        Assert.Null(app.Services.GetService<Aspire.Hosting.Dcp.DcpHost>());
        Assert.Null(app.Services.GetService<Aspire.Hosting.Dcp.IDcpExecutor>());
        Assert.Null(app.Services.GetService<Aspire.Hosting.Orchestrator.ApplicationOrchestrator>());
    }

    [Fact]
    public async Task VerifyInspectModeDoesNotExecutePublisher()
    {
        // Verify that publishers are not executed in inspect mode
        using var builder = TestDistributedApplicationBuilder
            .Create(["--operation", "inspect"])
            .WithTestAndResourceLogging(outputHelper);

        var publisherExecuted = false;
        builder.Eventing.Subscribe<BeforePublishEvent>((e, ct) => {
            publisherExecuted = true;
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        
        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);
        await app.StopAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        // Publisher should not have executed
        Assert.False(publisherExecuted);
    }
}

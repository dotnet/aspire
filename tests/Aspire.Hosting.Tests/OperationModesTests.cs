// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Tests;

public class OperationModesTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task EnsureWhenAppHostIsRunWithoutAnyArgsThatItDefaultsToRunMode()
    {
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
    public async Task EnsureWhenAppHostIsRunPublisherAndOutputPathSwitchThatItIsInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder
            .Create(["--publisher", "manifest", "--output-path", "test-output-path"])
            .WithTestAndResourceLogging(outputHelper);
        

        // TOOD: This won't work because this event does not fire in publish mode. We need
        //       another way to get at this internal state.
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

        Assert.Equal(DistributedApplicationOperation.Publish, context.Operation);
        Assert.True(context.IsPublishMode);
    }

    [Fact]
    public void TestOperationModes3()
    {
        // Optional explicit run mode: AppHost.exe --operation run
        Assert.True(false);

    }
    
    [Fact]
    public void TestOperationModes4()
    {
        // Optional explicit run mode: AppHost.exe --operation publish --publisher manifest --output-path
        Assert.True(false);

    }
    
    [Fact]
    public void TestOperationModes5()
    {
        // Mandatory inspect mode: AppHost.exe --operation inspect
        Assert.True(false);

    }
}
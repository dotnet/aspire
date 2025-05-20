// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Azure.Tests;

public class AzureFunctionsDurableTaskTests
{
    [Fact]
    public async Task AddDurableTaskScheduler()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddDurableTaskScheduler("scheduler");

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var scheduler = model.Resources.OfType<DurableTaskSchedulerResource>().Single();

        Assert.Null(scheduler.Authentication);
        Assert.Null(scheduler.ClientId);
        Assert.False(scheduler.IsEmulator);
        Assert.Null(scheduler.SchedulerEndpoint);
        Assert.Null(scheduler.SchedulerName);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await scheduler.ConnectionStringExpression.GetValueAsync(CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await scheduler.SchedulerEndpointExpression.GetValueAsync(CancellationToken.None));

        Assert.Null(scheduler.SubscriptionIdExpression);
        
        Assert.Equal(DurableTaskConstants.Scheduler.Dashboard.Endpoint.ToString(), await (scheduler as IResourceWithDashboard).DashboardEndpointExpression.GetValueAsync(CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await scheduler.DashboardSchedulerEndpointExpression.GetValueAsync(CancellationToken.None));
        Assert.Equal("scheduler", await scheduler.SchedulerNameExpression.GetValueAsync(CancellationToken.None));
    }

    [Fact]
    public async Task AddDurableTaskSchedulerWithConfiguration()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddDurableTaskScheduler(
            "scheduler",
            options =>
            {
                options.Resource.Authentication = "TestAuthentication";
                options.Resource.ClientId = "TestClientId";
                options.Resource.SchedulerEndpoint = new Uri("https://scheduler.test.io");
                options.Resource.SchedulerName = "TestSchedulerName";                
            });

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var scheduler = model.Resources.OfType<DurableTaskSchedulerResource>().Single();

        Assert.Equal("TestAuthentication", scheduler.Authentication);
        Assert.Equal("TestClientId", scheduler.ClientId);
        Assert.False(scheduler.IsEmulator);
        Assert.Equal(new Uri("https://scheduler.test.io"), scheduler.SchedulerEndpoint);
        Assert.Equal("TestSchedulerName", scheduler.SchedulerName);

        Assert.Equal("Endpoint=https://scheduler.test.io/;Authentication=TestAuthentication;ClientID=TestClientId", await scheduler.ConnectionStringExpression.GetValueAsync(CancellationToken.None));
        Assert.Equal("https://scheduler.test.io/", await scheduler.SchedulerEndpointExpression.GetValueAsync(CancellationToken.None));

        Assert.Null(scheduler.SubscriptionIdExpression);
        
        Assert.Equal("https://dashboard.durabletask.io/".ToString(), await (scheduler as IResourceWithDashboard).DashboardEndpointExpression.GetValueAsync(CancellationToken.None));
        Assert.Equal("https://scheduler.test.io/", scheduler.DashboardSchedulerEndpointExpression.ValueExpression);
        Assert.Equal("TestSchedulerName", await scheduler.SchedulerNameExpression.GetValueAsync(CancellationToken.None));
    }

    [Fact]
    public async Task AddDurableTaskSchedulerAsExisting()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder
            .AddDurableTaskScheduler("scheduler")
            .RunAsExisting("Endpoint=https://scheduler.test.io/;Authentication=TestAuth");

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var scheduler = model.Resources.OfType<DurableTaskSchedulerResource>().Single();

        Assert.Equal("TestAuth", scheduler.Authentication);
        Assert.Null(scheduler.ClientId);
        Assert.False(scheduler.IsEmulator);
        Assert.Equal(new Uri("https://scheduler.test.io/"), scheduler.SchedulerEndpoint);
        Assert.Null(scheduler.SchedulerName);

        Assert.Equal("Endpoint=https://scheduler.test.io/;Authentication=TestAuth", await scheduler.ConnectionStringExpression.GetValueAsync(CancellationToken.None));
        Assert.Equal("https://scheduler.test.io/", await scheduler.SchedulerEndpointExpression.GetValueAsync(CancellationToken.None));

        Assert.Null(scheduler.SubscriptionIdExpression);
        
        Assert.Equal("https://dashboard.durabletask.io/".ToString(), await (scheduler as IResourceWithDashboard).DashboardEndpointExpression.GetValueAsync(CancellationToken.None));
        Assert.Equal("https://scheduler.test.io/", scheduler.DashboardSchedulerEndpointExpression.ValueExpression);
        Assert.Equal("scheduler", await scheduler.SchedulerNameExpression.GetValueAsync(CancellationToken.None));
    }

    [Fact]
    public async Task AddDurableTaskSchedulerAsEmulator()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder
            .AddDurableTaskScheduler("scheduler")
            .RunAsEmulator();

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var scheduler = model.Resources.OfType<DurableTaskSchedulerResource>().Single();

        Assert.Equal("None", scheduler.Authentication);
        Assert.Null(scheduler.ClientId);
        Assert.True(scheduler.IsEmulator);
        Assert.Null(scheduler.SchedulerEndpoint);
        Assert.Null(scheduler.SchedulerName);

        Assert.Equal("Endpoint={scheduler.bindings.worker.url}/;Authentication=None", scheduler.ConnectionStringExpression.ValueExpression);
        Assert.Equal("{scheduler.bindings.worker.url}/", scheduler.SchedulerEndpointExpression.ValueExpression);

        Assert.Null(scheduler.SubscriptionIdExpression);
        
        Assert.Equal("{scheduler.bindings.dashboard.url}/", (scheduler as IResourceWithDashboard).DashboardEndpointExpression.ValueExpression);
        Assert.Equal("{scheduler.bindings.dashboard.url}/api/", scheduler.DashboardSchedulerEndpointExpression.ValueExpression);
        Assert.Equal("default", await scheduler.SchedulerNameExpression.GetValueAsync(CancellationToken.None));

        Assert.True(scheduler.TryGetLastAnnotation<ContainerImageAnnotation>(out var imageAnnotation));

        Assert.Equal("mcr.microsoft.com/dts", imageAnnotation.Registry);
        Assert.Equal("dts-emulator", imageAnnotation.Image);
        Assert.Equal("latest", imageAnnotation.Tag);

        Assert.True(scheduler.TryGetEnvironmentVariables(out var environmentVariables));

        EnvironmentCallbackContext context = new(builder.ExecutionContext);

        foreach (var environmentVariable in environmentVariables)
        {
            await environmentVariable.Callback(context);
        }

        // NOTE: If no task hub names are specified, no variable should be set as the default task hub name.
        Assert.False(context.EnvironmentVariables.TryGetValue("DTS_TASK_HUB_NAMES", out var taskHubNames));
    }

    [Fact]
    public async Task AddDurableTaskSchedulerAsEmulatorWithDynamicTaskhubs()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var schedulerBuilder = builder
            .AddDurableTaskScheduler("scheduler")
            .RunAsEmulator(
                options =>
                {
                    options.WithDynamicTaskHubs();
                });

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var scheduler = model.Resources.OfType<DurableTaskSchedulerResource>().Single();

        Assert.True(scheduler.TryGetEnvironmentVariables(out var environmentVariables));

        EnvironmentCallbackContext context = new(builder.ExecutionContext);

        foreach (var environmentVariable in environmentVariables)
        {
            await environmentVariable.Callback(context);
        }

        Assert.True(context.EnvironmentVariables.TryGetValue("DTS_USE_DYNAMIC_TASK_HUBS", out var useDynamicTaskHubs));
        Assert.Equal("true", useDynamicTaskHubs);
    }

    [Fact]
    public async Task AddDurableTaskSchedulerAsEmulatorWithTaskhub()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var schedulerBuilder = builder
            .AddDurableTaskScheduler("scheduler")
            .RunAsEmulator();

        schedulerBuilder.AddTaskHub("taskhub1");
        schedulerBuilder.AddTaskHub("taskhub2").WithTaskHubName("taskhub2a");

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var scheduler = model.Resources.OfType<DurableTaskSchedulerResource>().Single();

        Assert.True(scheduler.TryGetEnvironmentVariables(out var environmentVariables));

        EnvironmentCallbackContext context = new(builder.ExecutionContext);

        foreach (var environmentVariable in environmentVariables)
        {
            await environmentVariable.Callback(context);
        }

        Assert.True(context.EnvironmentVariables.TryGetValue("DTS_TASK_HUB_NAMES", out var taskHubNameString));

        var taskHubNames =
            taskHubNameString
                .ToString()
                !.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .OrderBy(x => x);

        Assert.Equal(taskHubNames, [ "taskhub1", "taskhub2a" ]);

        var taskHub1 = model.Resources.OfType<DurableTaskHubResource>().Single(x => x.Name == "taskhub1");

        Assert.Equal("Endpoint={scheduler.bindings.worker.url}/;Authentication=None;TaskHub=taskhub1", taskHub1.ConnectionStringExpression.ValueExpression);
        Assert.Equal("{scheduler.bindings.dashboard.url}/subscriptions/default/schedulers/default/taskhubs/taskhub1", (taskHub1 as IResourceWithDashboard).DashboardEndpointExpression.ValueExpression);

        var taskHub2 = model.Resources.OfType<DurableTaskHubResource>().Single(x => x.Name == "taskhub2");

        Assert.Equal("Endpoint={scheduler.bindings.worker.url}/;Authentication=None;TaskHub=taskhub2a", taskHub2.ConnectionStringExpression.ValueExpression);
        Assert.Equal("{scheduler.bindings.dashboard.url}/subscriptions/default/schedulers/default/taskhubs/taskhub2a", (taskHub2 as IResourceWithDashboard).DashboardEndpointExpression.ValueExpression);
    }

    [RequiresDocker]
    [Fact]
    public async Task ResourceStartsAndRespondsOk()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        using var builder = TestDistributedApplicationBuilder.Create();

        var scheduler =
            builder.AddDurableTaskScheduler("scheduler")
                   .RunAsEmulator();

        using var app = builder.Build();

        await app.StartAsync(cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(scheduler.Resource.Name, KnownResourceStates.Running, cts.Token);

        using var client = app.CreateHttpClient(scheduler.Resource.Name, "dashboard");

        using var response = await client.SendAsync(
            new()
            {
                Headers =
                {
                    { "x-taskhub", "default" },
                },
                RequestUri = new Uri("/api/v1/taskhubs/ping", UriKind.Relative),
                Method = HttpMethod.Get
            }, cts.Token);

        response.EnsureSuccessStatusCode();
    }
}
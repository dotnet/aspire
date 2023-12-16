// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Tests;

public class DistributedApplicationTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    // Primary constructors don't get ITestOutputHelper injected
    public DistributedApplicationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task RegisteredLifecycleHookIsExecutedWhenRunAsynchronously()
    {
        var exceptionMessage = "Exception from lifecycle hook to prove it ran!";

        var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLifecycleHook((sp) =>
        {
            return new CallbackLifecycleHook((appModel, cancellationToken) =>
            {
                Assert.NotNull(appModel);

                throw new DistributedApplicationException(exceptionMessage);
            });
        });

        var ex = await Assert.ThrowsAsync<DistributedApplicationException>(async () =>
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMinutes(1));
            await testProgram.RunAsync(cts.Token);
        });

        Assert.Equal(exceptionMessage, ex.Message);
    }

    [Fact]
    public async Task MultipleRegisteredLifecycleHooksAreExecuted()
    {
        var exceptionMessage = "Exception from lifecycle hook to prove it ran!";

        var signal = (FirstHookExecuted: false, SecondHookExecuted: false);

        var testProgram = CreateTestProgram();

        // Lifecycle hook 1
        testProgram.AppBuilder.Services.AddLifecycleHook((sp) =>
        {
            return new CallbackLifecycleHook((app, cancellationToken) =>
            {
                signal.FirstHookExecuted = true;
                return Task.CompletedTask;
            });
        });

        // Lifecycle hook 2
        testProgram.AppBuilder.Services.AddLifecycleHook((sp) =>
        {
            return new CallbackLifecycleHook((app, cancellationToken) =>
            {
                signal.SecondHookExecuted = true;

                // We still want to throw on the second one to block startup.
                throw new DistributedApplicationException(exceptionMessage);
            });
        });

        var ex = await Assert.ThrowsAsync<DistributedApplicationException>(async () =>
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMinutes(1));
            await testProgram.RunAsync(cts.Token);
        });

        Assert.Equal(exceptionMessage, ex.Message);
        Assert.True(signal.FirstHookExecuted);
        Assert.True(signal.SecondHookExecuted);
    }

    [Fact]
    public void RegisteredLifecycleHookIsExecutedWhenRunSynchronously()
    {
        var exceptionMessage = "Exception from lifecycle hook to prove it ran!";

        var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLifecycleHook((sp) =>
        {
            return new CallbackLifecycleHook((appModel, cancellationToken) =>
            {
                Assert.NotNull(appModel);

                throw new DistributedApplicationException(exceptionMessage);
            });
        });

        var ex = Assert.Throws<AggregateException>(testProgram.Run);
        Assert.IsType<DistributedApplicationException>(ex.InnerExceptions.First());
        Assert.Equal(exceptionMessage, ex.InnerExceptions.First().Message);
    }

    [Fact]
    public async Task TryAddWillNotAddTheSameLifecycleHook()
    {
        var exceptionMessage = "Exception from lifecycle hook to prove it ran!";

        var signal = (FirstHookExecuted: false, SecondHookExecuted: false);

        var testProgram = CreateTestProgram();

        // Lifecycle hook 1
        testProgram.AppBuilder.Services.TryAddLifecycleHook((sp) =>
        {
            return new CallbackLifecycleHook((app, cancellationToken) =>
            {
                signal.FirstHookExecuted = true;
                return Task.CompletedTask;
            });
        });

        // Lifecycle hook 2
        testProgram.AppBuilder.Services.TryAddLifecycleHook((sp) =>
        {
            return new CallbackLifecycleHook((app, cancellationToken) =>
            {
                signal.SecondHookExecuted = true;

                // We still want to throw on the second one to block startup.
                throw new DistributedApplicationException(exceptionMessage);
            });
        });

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMinutes(1));
        await using var app = testProgram.Build();
        await app.StartAsync(cts.Token);

        Assert.True(signal.FirstHookExecuted);
        Assert.False(signal.SecondHookExecuted);
    }

    [LocalOnlyFact]
    public async Task AllocatedPortsAssignedAfterHookRuns()
    {
        var testProgram = CreateTestProgram();
        var tcs = new TaskCompletionSource<DistributedApplicationModel>(TaskCreationOptions.RunContinuationsAsynchronously);
        testProgram.AppBuilder.Services.AddLifecycleHook(sp => new CheckAllocatedEndpointsLifecycleHook(tcs));

        await using var app = testProgram.Build();

        await app.StartAsync();

        var appModel = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        foreach (var item in appModel.Resources)
        {
            if ((item is ContainerResource || item is ProjectResource || item is ExecutableResource) && item.TryGetServiceBindings(out _))
            {
                Assert.True(item.TryGetAllocatedEndPoints(out var endpoints));
                Assert.NotEmpty(endpoints);
            }
        }
    }

    private sealed class CheckAllocatedEndpointsLifecycleHook(TaskCompletionSource<DistributedApplicationModel> tcs) : IDistributedApplicationLifecycleHook
    {
        public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
        {
            tcs.TrySetResult(appModel);

            return Task.CompletedTask;
        }
    }

    [LocalOnlyFact]
    public async Task TestProjectStartsAndStopsCleanly()
    {
        var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.Services
            .AddHttpClient()
            .ConfigureHttpClientDefaults(b =>
            {
                b.UseSocketsHttpHandler((handler, sp) => handler.PooledConnectionLifetime = TimeSpan.FromSeconds(5));
            });

        await using var app = testProgram.Build();

        var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        await app.StartAsync(cts.Token);

        // Make sure each service is running
        await testProgram.ServiceABuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceBBuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceCBuilder.HttpGetPidAsync(client, "http", cts.Token);
    }

    [LocalOnlyFact]
    public async Task TestPortOnServiceBindingAnnotationAndAllocatedEndpointAnnotationMatch()
    {
        var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.Services
            .AddHttpClient()
            .ConfigureHttpClientDefaults(b =>
            {
                b.UseSocketsHttpHandler((handler, sp) => handler.PooledConnectionLifetime = TimeSpan.FromSeconds(5));
            });

        await using var app = testProgram.Build();

        var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        await app.StartAsync(cts.Token);

        // Make sure each service is running
        await testProgram.ServiceABuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceBBuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceCBuilder.HttpGetPidAsync(client, "http", cts.Token);

        foreach (var projectBuilders in testProgram.ServiceProjectBuilders)
        {
            var serviceBinding = projectBuilders.Resource.Annotations.OfType<ServiceBindingAnnotation>().Single();
            var allocatedEndpoint = projectBuilders.Resource.Annotations.OfType<AllocatedEndpointAnnotation>().Single();

            Assert.Equal(serviceBinding.Port, allocatedEndpoint.Port);
        }
    }

    [LocalOnlyFact]
    public async Task TestPortOnServiceBindingAnnotationAndAllocatedEndpointAnnotationMatchForReplicatedServices()
    {
        var testProgram = CreateTestProgram();

        foreach (var serviceBuilder in testProgram.ServiceProjectBuilders)
        {
            serviceBuilder.WithReplicas(2);
        }

        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.Services
            .AddHttpClient()
            .ConfigureHttpClientDefaults(b =>
            {
                b.UseSocketsHttpHandler((handler, sp) => handler.PooledConnectionLifetime = TimeSpan.FromSeconds(5));
            });

        await using var app = testProgram.Build();

        var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        await app.StartAsync(cts.Token);

        // Make sure each service is running
        await testProgram.ServiceABuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceBBuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceCBuilder.HttpGetPidAsync(client, "http", cts.Token);

        foreach (var projectBuilders in testProgram.ServiceProjectBuilders)
        {
            var serviceBinding = projectBuilders.Resource.Annotations.OfType<ServiceBindingAnnotation>().Single();
            var allocatedEndpoint = projectBuilders.Resource.Annotations.OfType<AllocatedEndpointAnnotation>().Single();

            Assert.Equal(serviceBinding.Port, allocatedEndpoint.Port);
        }
    }

    [LocalOnlyFact]
    public async Task TestServicesWithMultipleReplicas()
    {
        var replicaCount = 3;

        var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.Services
            .AddHttpClient()
            .ConfigureHttpClientDefaults(b =>
            {
                b.UseSocketsHttpHandler((handler, sp) => handler.PooledConnectionLifetime = TimeSpan.FromSeconds(5));
            });

        testProgram.ServiceBBuilder.WithReplicas(replicaCount);

        await using var app = testProgram.Build();

        var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        await app.StartAsync(cts.Token);

        // Make sure services A and C are running
        await testProgram.ServiceABuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceCBuilder.HttpGetPidAsync(client, "http", cts.Token);

        // We should get 3 distinct PIDs from service B
        Dictionary<int, bool> pids = [];
        while (true)
        {
            var pid = await testProgram.ServiceBBuilder.HttpGetPidAsync(client, "http", cts.Token);
            if (!string.IsNullOrEmpty(pid))
            {
                pids[int.Parse(pid, CultureInfo.InvariantCulture)] = true;
                if (pids.Count == replicaCount)
                {
                    break; // Success! We heard from all 3 replicas.
                }
            }
            await Task.Delay(100, cts.Token);
        }
    }

    [LocalOnlyFact]
    public async Task VerifyHealthyOnIntegrationServiceA()
    {
        var testProgram = CreateTestProgram(includeIntegrationServices: true);
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.Services
            .AddHttpClient()
            .ConfigureHttpClientDefaults(b =>
            {
                b.UseSocketsHttpHandler((handler, sp) => handler.PooledConnectionLifetime = TimeSpan.FromSeconds(5));
            });

        await using var app = testProgram.Build();

        var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        await app.StartAsync(cts.Token);

        // Make sure all services are running
        await testProgram.ServiceABuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceBBuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceCBuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.IntegrationServiceABuilder!.HttpGetPidAsync(client, "http", cts.Token);

        // We wait until timeout for the /health endpoint to return successfully. We assume
        // that components wired up into this project have health checks enabled.
        await testProgram.IntegrationServiceABuilder!.WaitForHealthyStatus(client, "http", cts.Token);
    }

    [LocalOnlyFact("node")]
    public async Task VerifyNodeAppWorks()
    {
        var testProgram = CreateTestProgram(includeNodeApp: true);
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.Services
            .AddHttpClient()
            .ConfigureHttpClientDefaults(b =>
            {
                b.UseSocketsHttpHandler((handler, sp) => handler.PooledConnectionLifetime = TimeSpan.FromSeconds(5));
                b.AddStandardResilienceHandler();
            });

        await using var app = testProgram.Build();

        var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        await app.StartAsync(cts.Token);

        var response0 = await testProgram.NodeAppBuilder!.HttpGetWithRetryAsync(client, "http", "/", cts.Token);
        var response1 = await testProgram.NpmAppBuilder!.HttpGetWithRetryAsync(client, "http", "/", cts.Token);

        Assert.Equal("Hello from node!", response0);
        Assert.Equal("Hello from node!", response1);
    }

    [LocalOnlyFact("docker")]
    public async Task VerifyDockerAppWorks()
    {
        var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.AddContainer("redis-cli", "redis")
            .WithArgs("redis-cli", "-h", "host.docker.internal", "-p", "9999", "MONITOR");

        await using var app = testProgram.Build();

        await app.StartAsync();

        var s = app.Services.GetRequiredService<KubernetesService>();
        var list = await s.ListAsync<Container>();

        Assert.Collection(list,
            item =>
            {
                Assert.Equal("redis:latest", item.Spec.Image);
                Assert.Equal(["redis-cli", "-h", "host.docker.internal", "-p", "9999", "MONITOR"], item.Spec.Args);
            });

        await app.StopAsync();
    }

    private static TestProgram CreateTestProgram(string[]? args = null, bool includeIntegrationServices = false, bool includeNodeApp = false) =>
        TestProgram.Create<DistributedApplicationTests>(args, includeIntegrationServices: includeIntegrationServices, includeNodeApp: includeNodeApp);
}

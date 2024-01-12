// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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
            if ((item is ContainerResource || item is ProjectResource || item is ExecutableResource) && item.TryGetEndpoints(out _))
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

        // Give the server some time to be ready to handle requests to
        // minimize the amount of retries the clients have to do (and log).

        await Task.Delay(1000, cts.Token);

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

    [LocalOnlyFact("docker")]
    public async Task SpecifyingEnvPortInEndpointFlowsToEnv()
    {
        var testProgram = CreateTestProgram(includeNodeApp: true);

        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.ServiceABuilder
            .WithEndpoint(scheme: "http", name: "http0", env: "PORT0");

        testProgram.AppBuilder.AddContainer("redis0", "redis")
            .WithEndpoint(containerPort: 6379, name: "tcp", env: "REDIS_PORT");

        await using var app = testProgram.Build();

        var kubernetes = app.Services.GetRequiredService<KubernetesService>();

        await app.StartAsync();

        using var cts = new CancellationTokenSource(Debugger.IsAttached ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10));
        var token = cts.Token;

        var redisContainer = await KubernetesHelper.GetResourceByNameAsync<Container>(kubernetes, "redis0", r => r.Status?.EffectiveEnv is not null, token);
        Assert.NotNull(redisContainer);

        var serviceA = await KubernetesHelper.GetResourceByNameAsync<Executable>(kubernetes, "servicea", r => r.Status?.EffectiveEnv is not null, token);
        Assert.NotNull(serviceA);

        var nodeApp = await KubernetesHelper.GetResourceByNameAsync<Executable>(kubernetes, "nodeapp", r => r.Status?.EffectiveEnv is not null, token);
        Assert.NotNull(nodeApp);

        string? GetEnv(IEnumerable<EnvVar>? envVars, string name)
        {
            Assert.NotNull(envVars);
            return Assert.Single(envVars.Where(e => e.Name == name)).Value;
        };

        Assert.Equal("redis:latest", redisContainer.Spec.Image);
        Assert.Equal("{{- portForServing \"redis0\" }}", GetEnv(redisContainer.Spec.Env, "REDIS_PORT"));
        Assert.Equal("6379", GetEnv(redisContainer.Status!.EffectiveEnv, "REDIS_PORT"));

        Assert.Equal("{{- portForServing \"servicea_http0\" }}", GetEnv(serviceA.Spec.Env, "PORT0"));
        var serviceAPortValue = GetEnv(serviceA.Status!.EffectiveEnv, "PORT0");
        Assert.False(string.IsNullOrEmpty(serviceAPortValue));
        Assert.NotEqual(0, int.Parse(serviceAPortValue, CultureInfo.InvariantCulture));

        Assert.Equal("{{- portForServing \"nodeapp\" }}", GetEnv(nodeApp.Spec.Env, "PORT"));
        var nodeAppPortValue = GetEnv(nodeApp.Status!.EffectiveEnv, "PORT");
        Assert.False(string.IsNullOrEmpty(nodeAppPortValue));
        Assert.NotEqual(0, int.Parse(nodeAppPortValue, CultureInfo.InvariantCulture));

        await app.StopAsync();
    }

    [LocalOnlyFact("docker")]
    public async Task VerifyDockerWithEntrypointWorks()
    {
        var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.AddContainer("redis-cli", "redis")
            .WithEntrypoint("bob");

        await using var app = testProgram.Build();

        await app.StartAsync();

        var s = app.Services.GetRequiredService<KubernetesService>();

        using var cts = new CancellationTokenSource(Debugger.IsAttached ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10));
        var token = cts.Token;

        var redisContainer = await KubernetesHelper.GetResourceByNameAsync<Container>(s, "redis-cli",
            r => r.Status?.State == ContainerState.FailedToStart && (r.Status?.Message.Contains("bob") ?? false),
            token);

        Assert.NotNull(redisContainer);
        Assert.Equal("redis:latest", redisContainer.Spec.Image);
        Assert.Equal("bob", redisContainer.Spec.Command);

        await app.StopAsync();
    }

    private static TestProgram CreateTestProgram(string[]? args = null, bool includeIntegrationServices = false, bool includeNodeApp = false) =>
        TestProgram.Create<DistributedApplicationTests>(args, includeIntegrationServices: includeIntegrationServices, includeNodeApp: includeNodeApp);
}

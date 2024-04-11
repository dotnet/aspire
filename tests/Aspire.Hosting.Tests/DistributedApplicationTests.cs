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
using Xunit.Sdk;

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

        using var testProgram = CreateTestProgram();
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

        using var testProgram = CreateTestProgram();

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

        using var testProgram = CreateTestProgram();
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
    public void TryAddWillNotAddTheSameLifecycleHook()
    {
        using var testProgram = CreateTestProgram();

        var callback1 = (IServiceProvider sp) => new DummyLifecycleHook();
        testProgram.AppBuilder.Services.TryAddLifecycleHook(callback1);

        var callback2 = (IServiceProvider sp) => new DummyLifecycleHook();
        testProgram.AppBuilder.Services.TryAddLifecycleHook(callback2);

        var lifecycleHookDescriptors = testProgram.AppBuilder.Services.Where(sd => sd.ServiceType == typeof(IDistributedApplicationLifecycleHook));

        Assert.Single(lifecycleHookDescriptors.Where(sd => sd.ImplementationFactory == callback1));
        Assert.Empty(lifecycleHookDescriptors.Where(sd => sd.ImplementationFactory == callback2));
    }

    [LocalOnlyFact]
    public async Task AllocatedPortsAssignedAfterHookRuns()
    {
        using var testProgram = CreateTestProgram();
        var tcs = new TaskCompletionSource<DistributedApplicationModel>(TaskCreationOptions.RunContinuationsAsynchronously);
        testProgram.AppBuilder.Services.AddLifecycleHook(sp => new CheckAllocatedEndpointsLifecycleHook(tcs));

        await using var app = testProgram.Build();

        await app.StartAsync();

        var appModel = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        foreach (var item in appModel.Resources)
        {
            if (item is IResourceWithEndpoints resourceWithEndpoints)
            {
                Assert.True(resourceWithEndpoints.GetEndpoints().All(e => e.IsAllocated));
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

        using var testProgram = CreateTestProgram();
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
        using var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.AddContainer("redis-cli", "redis")
            .WithArgs("redis-cli", "-h", "host.docker.internal", "-p", "9999", "MONITOR")
            .WithContainerRunArgs("--add-host", "testlocalhost:127.0.0.1");

        await using var app = testProgram.Build();

        await app.StartAsync();

        var s = app.Services.GetRequiredService<IKubernetesService>();
        var list = await s.ListAsync<Container>();

        Assert.Collection(list,
            item =>
            {
                Assert.Equal("redis:latest", item.Spec.Image);
                Assert.Equal(["redis-cli", "-h", "host.docker.internal", "-p", "9999", "MONITOR"], item.Spec.Args);
                Assert.Equal(["--add-host", "testlocalhost:127.0.0.1"], item.Spec.RunArgs);
            });

        await app.StopAsync();
    }

    [LocalOnlyFact("docker")]
    public async Task SpecifyingEnvPortInEndpointFlowsToEnv()
    {
        using var testProgram = CreateTestProgram(includeNodeApp: true);

        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.ServiceABuilder
            .WithHttpEndpoint(name: "http0", env: "PORT0");

        testProgram.AppBuilder.AddContainer("redis0", "redis")
            .WithEndpoint(targetPort: 6379, name: "tcp", env: "REDIS_PORT");

        await using var app = testProgram.Build();

        var kubernetes = app.Services.GetRequiredService<IKubernetesService>();

        await app.StartAsync();

        using var cts = new CancellationTokenSource(Debugger.IsAttached ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10));
        var token = cts.Token;

        var redisContainer = await KubernetesHelper.GetResourceByNameAsync<Container>(kubernetes, "redis0", r => r.Status?.EffectiveEnv is not null, token);
        Assert.NotNull(redisContainer);

        var serviceA = await KubernetesHelper.GetResourceByNameAsync<Executable>(kubernetes, "servicea", r => r.Status?.EffectiveEnv is not null, token);
        Assert.NotNull(serviceA);

        var nodeApp = await KubernetesHelper.GetResourceByNameAsync<Executable>(kubernetes, "nodeapp", r => r.Status?.EffectiveEnv is not null, token);
        Assert.NotNull(nodeApp);

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

        static string? GetEnv(IEnumerable<EnvVar>? envVars, string name)
        {
            Assert.NotNull(envVars);
            return Assert.Single(envVars.Where(e => e.Name == name)).Value;
        }
    }

    [LocalOnlyFact("docker")]
    public async Task StartAsync_DashboardAuthConfig_PassedToDashboardProcess()
    {
        var browserToken = "ThisIsATestToken";
        var args = new string[] {
            "ASPNETCORE_URLS=http://localhost:0",
            "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL=http://localhost:0",
            $"DOTNET_DASHBOARD_FRONTEND_BROWSERTOKEN={browserToken}"
        };
        using var testProgram = CreateTestProgram(args: args, disableDashboard: false);

        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        await using var app = testProgram.Build();

        var kubernetes = app.Services.GetRequiredService<IKubernetesService>();

        await app.StartAsync();

        using var cts = new CancellationTokenSource(Debugger.IsAttached ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10));
        var token = cts.Token;

        var aspireDashboard = await KubernetesHelper.GetResourceByNameAsync<Executable>(kubernetes, "aspire-dashboard", r => r.Status?.EffectiveEnv is not null, token);
        Assert.NotNull(aspireDashboard);

        Assert.Equal("BrowserToken", GetEnv(aspireDashboard.Spec.Env, "DASHBOARD__FRONTEND__AUTHMODE"));
        Assert.Equal("ThisIsATestToken", GetEnv(aspireDashboard.Spec.Env, "DASHBOARD__FRONTEND__BROWSERTOKEN"));

        Assert.Equal("ApiKey", GetEnv(aspireDashboard.Spec.Env, "DASHBOARD__OTLP__AUTHMODE"));
        var keyBytes = Convert.FromHexString(GetEnv(aspireDashboard.Spec.Env, "DASHBOARD__OTLP__PRIMARYAPIKEY")!);
        Assert.Equal(16, keyBytes.Length);

        await app.StopAsync();

        static string? GetEnv(IEnumerable<EnvVar>? envVars, string name)
        {
            Assert.NotNull(envVars);
            return Assert.Single(envVars.Where(e => e.Name == name)).Value;
        }
    }

    [LocalOnlyFact("docker")]
    public async Task StartAsync_UnsecuredAllowAnonymous_PassedToDashboardProcess()
    {
        var args = new string[] {
            "ASPNETCORE_URLS=http://localhost:0",
            "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL=http://localhost:0",
            "DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true"
        };
        using var testProgram = CreateTestProgram(args: args, disableDashboard: false);

        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        await using var app = testProgram.Build();

        var kubernetes = app.Services.GetRequiredService<IKubernetesService>();

        await app.StartAsync();

        using var cts = new CancellationTokenSource(Debugger.IsAttached ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10));
        var token = cts.Token;

        var aspireDashboard = await KubernetesHelper.GetResourceByNameAsync<Executable>(kubernetes, "aspire-dashboard", r => r.Status?.EffectiveEnv is not null, token);
        Assert.NotNull(aspireDashboard);

        Assert.Equal("Unsecured", GetEnv(aspireDashboard.Spec.Env, "DASHBOARD__FRONTEND__AUTHMODE"));
        Assert.Equal("Unsecured", GetEnv(aspireDashboard.Spec.Env, "DASHBOARD__OTLP__AUTHMODE"));

        await app.StopAsync();

        static string? GetEnv(IEnumerable<EnvVar>? envVars, string name)
        {
            Assert.NotNull(envVars);
            return Assert.Single(envVars.Where(e => e.Name == name)).Value;
        }
    }

    [LocalOnlyFact("docker")]
    public async Task VerifyDockerWithEntrypointWorks()
    {
        using var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.AddContainer("redis-cli", "redis")
            .WithEntrypoint("bob");

        await using var app = testProgram.Build();

        await app.StartAsync();

        var s = app.Services.GetRequiredService<IKubernetesService>();

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

    [LocalOnlyFact("docker")]
    public async Task VerifyDockerWithBindMountWorksWithAbsolutePaths()
    {
        using var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        var sourcePath = Path.GetFullPath("/etc/path-here");
        testProgram.AppBuilder.AddContainer("redis-cli", "redis")
            .WithBindMount(sourcePath, "path-here");

        await using var app = testProgram.Build();

        await app.StartAsync();

        var s = app.Services.GetRequiredService<IKubernetesService>();

        using var cts = new CancellationTokenSource(Debugger.IsAttached ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10));
        var token = cts.Token;

        var redisContainer = await KubernetesHelper.GetResourceByNameAsync<Container>(
                s,
                "redis-cli", r => r.Spec.VolumeMounts != null,
                token);

        Assert.NotNull(redisContainer.Spec.VolumeMounts);
        Assert.NotEmpty(redisContainer.Spec.VolumeMounts);
        Assert.Equal(sourcePath, redisContainer.Spec.VolumeMounts[0].Source);

        await app.StopAsync();
    }

    [LocalOnlyFact("docker")]
    public async Task VerifyDockerWithBindMountWorksWithRelativePaths()
    {
        using var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.AddContainer("redis-cli", "redis")
            .WithBindMount("etc/path-here", "path-here");

        await using var app = testProgram.Build();

        await app.StartAsync();

        var s = app.Services.GetRequiredService<IKubernetesService>();

        using var cts = new CancellationTokenSource(Debugger.IsAttached ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10));
        var token = cts.Token;

        var redisContainer = await KubernetesHelper.GetResourceByNameAsync<Container>(
            s,
            "redis-cli", r => r.Spec.VolumeMounts != null,
            token);

        Assert.NotNull(redisContainer.Spec.VolumeMounts);
        Assert.NotEmpty(redisContainer.Spec.VolumeMounts);
        Assert.NotEqual("etc/path-here", redisContainer.Spec.VolumeMounts[0].Source);
        Assert.True(Path.IsPathRooted(redisContainer.Spec.VolumeMounts[0].Source));

        await app.StopAsync();
    }

    [LocalOnlyFact("docker")]
    public async Task VerifyDockerWithVolumeWorksWithName()
    {
        using var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.AddContainer("redis-cli", "redis")
            .WithVolume("test-volume-name", "/path-here");

        await using var app = testProgram.Build();

        await app.StartAsync();

        var s = app.Services.GetRequiredService<IKubernetesService>();

        using var cts = new CancellationTokenSource(Debugger.IsAttached ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10));
        var token = cts.Token;

        var redisContainer = await KubernetesHelper.GetResourceByNameAsync<Container>(
                s,
                "redis-cli", r => r.Spec.VolumeMounts != null,
                token);

        Assert.NotNull(redisContainer.Spec.VolumeMounts);
        Assert.NotEmpty(redisContainer.Spec.VolumeMounts);
        Assert.Equal("test-volume-name", redisContainer.Spec.VolumeMounts[0].Source);

        await app.StopAsync();
    }

    [LocalOnlyFact("docker")]
    public async Task KubernetesHasResourceNameForContainersAndExes()
    {
        using var testProgram = CreateTestProgram(includeIntegrationServices: true, includeNodeApp: true);
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        await using var app = testProgram.Build();

        await app.StartAsync();

        var s = app.Services.GetRequiredService<IKubernetesService>();

        using var cts = new CancellationTokenSource(Debugger.IsAttached ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10));
        var token = cts.Token;

        var expectedExeResources = new HashSet<string>()
        {
            "servicea",
            "serviceb",
            "servicec",
            "workera",
            "nodeapp",
            "npmapp",
            "integrationservicea"
        };

        var expectedContainerResources = new HashSet<string>()
        {
            "redis",
            "postgres",
            "mongodb",
            "oracledatabase",
            "cosmos",
            "sqlserver",
            "mysql",
            "rabbitmq",
            "kafka"
        };

        await foreach (var resource in s.WatchAsync<Container>(cancellationToken: token))
        {
            Assert.True(resource.Item2.Metadata.Annotations.TryGetValue(Container.ResourceNameAnnotation, out var value));
            if (expectedContainerResources.Contains(value))
            {
                expectedContainerResources.Remove(value);
            }

            if (expectedContainerResources.Count == 0)
            {
                break;
            }
        }

        await foreach (var resource in s.WatchAsync<Executable>(cancellationToken: token))
        {
            Assert.True(resource.Item2.Metadata.Annotations.TryGetValue(Executable.ResourceNameAnnotation, out var value));
            if (expectedExeResources.Contains(value))
            {
                expectedExeResources.Remove(value);
            }

            if (expectedExeResources.Count == 0)
            {
                break;
            }
        }
    }

    [LocalOnlyFact("docker")]
    public async Task ReplicasAndProxylessEndpointThrows()
    {
        var testProgram = CreateTestProgram();
        testProgram.ServiceABuilder.WithReplicas(2).WithEndpoint("http", endpoint =>
        {
            endpoint.IsProxied = false;
        });
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        await using var app = testProgram.Build();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync());
        Assert.Equal("'servicea' specifies multiple replicas and at least one proxyless endpoint. These features do not work together.", ex.Message);
    }

    [LocalOnlyFact("docker")]
    public async Task ProxylessEndpointWithoutPortThrows()
    {
        var testProgram = CreateTestProgram();
        testProgram.ServiceABuilder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = null;
            endpoint.IsProxied = false;
        });
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        await using var app = testProgram.Build();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync());
        Assert.Equal("Service 'servicea' needs to specify a port for endpoint 'http' since it isn't using a proxy.", ex.Message);
    }

    [LocalOnlyFact("docker")]
    public async Task ProxylessEndpointWorks()
    {
        var testProgram = CreateTestProgram();

        testProgram.AppBuilder.Services
            .AddHttpClient()
            .ConfigureHttpClientDefaults(b =>
            {
                b.UseSocketsHttpHandler((handler, sp) => handler.PooledConnectionLifetime = TimeSpan.FromSeconds(5));
            });

        testProgram.ServiceABuilder
            .WithEndpoint("http", e =>
            {
                e.Port = 1234;
                e.IsProxied = false;
            });
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        await using var app = testProgram.Build();

        await app.StartAsync();

        var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

        var token = new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;

        while (true)
        {
            try
            {
                await client.GetStringAsync("http://localhost:1234/pid", token);
                break;
            }
            catch
            {
                await Task.Delay(100, token);
            }
        }

        // Check that endpoint from launchsettings doesn't work
        await Assert.ThrowsAnyAsync<Exception>(() => client.GetStringAsync("http://localhost:5156/pid"));
    }

    [LocalOnlyFact("docker")]
    public async Task ProxylessAndProxiedEndpointBothWorkOnSameResource()
    {
        var testProgram = CreateTestProgram();

        testProgram.AppBuilder.Services
            .AddHttpClient()
            .ConfigureHttpClientDefaults(b =>
            {
                b.UseSocketsHttpHandler((handler, sp) => handler.PooledConnectionLifetime = TimeSpan.FromSeconds(5));
            });

        testProgram.ServiceABuilder
            .WithEndpoint("http", e =>
            {
                e.Port = 1234;
                e.IsProxied = false;
            }, createIfNotExists: false)
            .WithEndpoint("https", e =>
            {
                e.UriScheme = "https";
                e.Port = 1543;
            }, createIfNotExists: true);

        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        await using var app = testProgram.Build();

        await app.StartAsync();

        var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

        var token = new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;

        var urls = string.Empty;
        while (true)
        {
            try
            {
                urls = await client.GetStringAsync("http://localhost:1234/urls", token);
                break;
            }
            catch
            {
                await Task.Delay(100, token);
            }
        }

        Assert.Contains("http://localhost:1234", urls);
        // https endpoint is proxied so app won't have this specific endpoint
        Assert.DoesNotContain("https://localhost:1543", urls);

        while (true)
        {
            try
            {
                var value = await client.GetStringAsync("https://localhost:1543/urls", token);
                Assert.Equal(urls, value);
                break;
            }
            catch (Exception ex) when (ex is not EqualException)
            {
                await Task.Delay(100, token);
            }
        }
    }

    [LocalOnlyFact("docker")]
    public async Task ProxylessContainerCanBeReferenced()
    {
        var builder = DistributedApplication.CreateBuilder(
            new DistributedApplicationOptions { DisableDashboard = true, AssemblyName = typeof(DistributedApplicationTests).Assembly.FullName });

        var redis = builder.AddRedis("redis", 1234).WithEndpoint("tcp", endpoint =>
        {
            endpoint.IsProxied = false;
        });
        var servicea = builder.AddProject<Projects.ServiceA>("servicea")
            .WithReference(redis);

        using var app = builder.Build();
        await app.StartAsync();

        var s = app.Services.GetRequiredService<IKubernetesService>();
        var exeList = await s.ListAsync<Executable>();

        var service = Assert.Single(exeList);
        var env = Assert.Single(service.Spec.Env!.Where(e => e.Name == "ConnectionStrings__redis"));
        Assert.Equal("localhost:1234", env.Value);

        var list = await s.ListAsync<Container>();
        var redisContainer = Assert.Single(list);
        Assert.Equal(1234, Assert.Single(redisContainer.Spec.Ports!).HostPort);

        await app.StopAsync();
    }

    [LocalOnlyFact("docker")]
    public async Task ProxylessContainerWithoutPortThrows()
    {
        var builder = DistributedApplication.CreateBuilder(
            new DistributedApplicationOptions { DisableDashboard = true, AssemblyName = typeof(DistributedApplicationTests).Assembly.FullName });

        var redis = builder.AddRedis("redis").WithEndpoint("tcp", endpoint =>
        {
            endpoint.IsProxied = false;
        });

        using var app = builder.Build();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync());
        Assert.Equal("Service 'redis' needs to specify a port for endpoint 'tcp' since it isn't using a proxy.", ex.Message);
    }

    [LocalOnlyFact("docker")]
    public async Task AfterResourcesCreatedLifecycleHookWorks()
    {
        var builder = DistributedApplication.CreateBuilder(
            new DistributedApplicationOptions { DisableDashboard = true, AssemblyName = typeof(DistributedApplicationTests).Assembly.FullName });

        builder.AddRedis("redis");
        builder.Services.TryAddLifecycleHook<KubernetesTestLifecycleHook>();

        using var app = builder.Build();

        var s = app.Services.GetRequiredService<IKubernetesService>();
        var lifecycles = app.Services.GetServices<IDistributedApplicationLifecycleHook>();
        var kubernetesLifecycle = (KubernetesTestLifecycleHook)lifecycles.Where(l => l.GetType() == typeof(KubernetesTestLifecycleHook)).First();
        kubernetesLifecycle.KubernetesService = s;

        await app.StartAsync();

        await kubernetesLifecycle.HooksCompleted;
    }

    private sealed class KubernetesTestLifecycleHook : IDistributedApplicationLifecycleHook
    {
        private readonly TaskCompletionSource _tcs = new();

        public IKubernetesService? KubernetesService { get; set; }

        public Task HooksCompleted => _tcs.Task;

        public async Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
        {
            Assert.Empty(await KubernetesService!.ListAsync<Container>(cancellationToken: cancellationToken));
        }

        public async Task AfterResourcesCreatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
        {
            Assert.NotEmpty(await KubernetesService!.ListAsync<Container>(cancellationToken: cancellationToken));
            _tcs.SetResult();
        }
    }

    private static TestProgram CreateTestProgram(string[]? args = null, bool includeIntegrationServices = false, bool includeNodeApp = false, bool disableDashboard = true) =>
        TestProgram.Create<DistributedApplicationTests>(args, includeIntegrationServices: includeIntegrationServices, includeNodeApp: includeNodeApp, disableDashboard: disableDashboard);
}

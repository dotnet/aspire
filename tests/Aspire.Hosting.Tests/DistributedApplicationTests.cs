// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.RegularExpressions;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Testing.Tests;
using Aspire.Hosting.Tests.Helpers;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using k8s.Models;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using TestConstants = Microsoft.AspNetCore.InternalTesting.TestConstants;

namespace Aspire.Hosting.Tests;

public class DistributedApplicationTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    private const string ReplicaIdRegex = @"[\w]+"; // Matches a replica ID that is part of a resource name.

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
        }).DefaultTimeout();

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
        }).DefaultTimeout();

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
        Assert.DoesNotContain(lifecycleHookDescriptors, sd => sd.ImplementationFactory == callback2);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task AllocatedPortsAssignedAfterHookRuns()
    {
        using var testProgram = CreateTestProgram();
        var tcs = new TaskCompletionSource<DistributedApplicationModel>(TaskCreationOptions.RunContinuationsAsynchronously);
        testProgram.AppBuilder.Services.AddLifecycleHook(sp => new CheckAllocatedEndpointsLifecycleHook(tcs));

        await using var app = testProgram.Build();

        await app.StartAsync().DefaultTimeout();

        var appModel = await tcs.Task.DefaultTimeout();

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

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task TestServicesWithMultipleReplicas()
    {
        var replicaCount = 3;

        using var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.ServiceBBuilder.WithReplicas(replicaCount);

        await using var app = testProgram.Build();

        var logger = app.Services.GetRequiredService<ILogger<DistributedApplicationTests>>();

        await app.StartAsync().DefaultTimeout();

        logger.LogInformation("Make sure services A and C are running");
        using var clientA = app.CreateHttpClient(testProgram.ServiceABuilder.Resource.Name, "http");
        using var clientC = app.CreateHttpClient(testProgram.ServiceCBuilder.Resource.Name, "http");

        await Task.WhenAll(clientA.GetStringAsync("/pid"), clientC.GetStringAsync("/pid")).DefaultTimeout(TestConstants.LongTimeoutDuration);

        // We should get 3 distinct PIDs from service B
        Dictionary<int, bool> pids = [];

        var uri = app.GetEndpoint(testProgram.ServiceBBuilder.Resource.Name, "http");

        var cts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource();
        while (!cts.IsCancellationRequested)
        {
            using var clientB = new HttpClient();
            var url = $"{uri}pid";
            logger.LogInformation("Calling PID API at {Url}", url);
            var pidText = await clientB.GetStringAsync(url).DefaultTimeout();
            if (!string.IsNullOrEmpty(pidText))
            {
                var pid = int.Parse(pidText, CultureInfo.InvariantCulture);
                if (pids.TryAdd(pid, true))
                {
                    logger.LogInformation("PID API returned new value: {PID}", pid);

                    if (pids.Count == replicaCount)
                    {
                        logger.LogInformation("Success! We heard from all {ReplicaCount} replicas.", replicaCount);
                        break;
                    }
                }
            }

            await Task.Delay(100);
        }

        Assert.Equal(3, pids.Count);
    }

    [Fact]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task VerifyDockerAppWorks()
    {
        using var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.AddContainer("redis-cli", "redis")
            .WithArgs("redis-cli", "-h", "host.docker.internal", "-p", "9999", "MONITOR")
            .WithContainerRuntimeArgs("--add-host", "testlocalhost:127.0.0.1");

        await using var app = testProgram.Build();

        await app.StartAsync().DefaultTimeout();

        var s = app.Services.GetRequiredService<IKubernetesService>();
        var list = await s.ListAsync<Container>().DefaultTimeout();

        Assert.Collection(list,
            item =>
            {
                Assert.Equal("redis:latest", item.Spec.Image);
                Assert.Equal(["redis-cli", "-h", "host.docker.internal", "-p", "9999", "MONITOR"], item.Spec.Args);
                Assert.Equal(["--add-host", "testlocalhost:127.0.0.1"], item.Spec.RunArgs);
            });

        await app.StopAsync().DefaultTimeout();
    }

    [Fact]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task VerifyContainerStopStartWorks()
    {
        using var testProgram = CreateTestProgram(randomizePorts: false);

        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.AddContainer("redis0", "redis")
            .WithEndpoint(targetPort: 6379, name: "tcp", env: "REDIS_PORT");

        await using var app = testProgram.Build();

        var kubernetes = app.Services.GetRequiredService<IKubernetesService>();
        var applicationExecutor = app.Services.GetRequiredService<ApplicationExecutor>();
        var suffix = app.Services.GetRequiredService<IOptions<DcpOptions>>().Value.ResourceNameSuffix;

        await app.StartAsync().DefaultTimeout();

        using var cts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        var token = cts.Token;

        var containerPattern = $"redis0-{ReplicaIdRegex}-{suffix}";
        var redisContainer = await KubernetesHelper.GetResourceByNameMatchAsync<Container>(kubernetes, containerPattern, r => r.Status?.State == ContainerState.Running, token).DefaultTimeout(TestConstants.LongTimeoutDuration);
        Assert.NotNull(redisContainer);

        await applicationExecutor.StopResourceAsync(redisContainer.Metadata.Name, token).DefaultTimeout();

        redisContainer = await KubernetesHelper.GetResourceByNameMatchAsync<Container>(kubernetes, containerPattern, r => r.Status?.State == ContainerState.Exited, token).DefaultTimeout(TestConstants.LongTimeoutDuration);
        Assert.NotNull(redisContainer);

        // TODO: Container start has issues in DCP. Waiting for fix.
        //await applicationExecutor.StartResourceAsync(redisContainer.Metadata.Name, token);

        //redisContainer = await KubernetesHelper.GetResourceByNameMatchAsync<Container>(kubernetes, containerPattern, r => r.Status?.State == ContainerState.Running, token);
        //Assert.NotNull(redisContainer);

        await app.StopAsync().DefaultTimeout();
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task VerifyExecutableStopStartWorks()
    {
        using var testProgram = CreateTestProgram(randomizePorts: false);

        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        await using var app = testProgram.Build();

        var kubernetes = app.Services.GetRequiredService<IKubernetesService>();
        var applicationExecutor = app.Services.GetRequiredService<ApplicationExecutor>();
        var suffix = app.Services.GetRequiredService<IOptions<DcpOptions>>().Value.ResourceNameSuffix;

        await app.StartAsync().DefaultTimeout();

        var executablePattern = $"servicea-{ReplicaIdRegex}-{suffix}";
        var serviceA = await KubernetesHelper.GetResourceByNameMatchAsync<Executable>(kubernetes, executablePattern, r => r.Status?.State == ExecutableState.Running).DefaultTimeout(TestConstants.LongTimeoutDuration);
        Assert.NotNull(serviceA);

        await applicationExecutor.StopResourceAsync(serviceA.Metadata.Name, CancellationToken.None).DefaultTimeout();

        serviceA = await KubernetesHelper.GetResourceByNameMatchAsync<Executable>(kubernetes, executablePattern, r => r.Status?.State == ExecutableState.Finished).DefaultTimeout(TestConstants.LongTimeoutDuration);
        Assert.NotNull(serviceA);

        await applicationExecutor.StartResourceAsync(serviceA.Metadata.Name, CancellationToken.None).DefaultTimeout();

        serviceA = await KubernetesHelper.GetResourceByNameMatchAsync<Executable>(kubernetes, executablePattern, r => r.Status?.State == ExecutableState.Running).DefaultTimeout(TestConstants.LongTimeoutDuration);
        Assert.NotNull(serviceA);

        await app.StopAsync().DefaultTimeout();
    }

    [Fact]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task SpecifyingEnvPortInEndpointFlowsToEnv()
    {
        using var testProgram = CreateTestProgram(randomizePorts: false);

        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.ServiceABuilder
            .WithHttpEndpoint(name: "http0", env: "PORT0");

        testProgram.AppBuilder.AddContainer("redis0", "redis")
            .WithEndpoint(targetPort: 6379, name: "tcp", env: "REDIS_PORT");

        testProgram.AppBuilder.AddNodeApp("nodeapp", "fakePath")
            .WithHttpEndpoint(port: 5031, env: "PORT");

        await using var app = testProgram.Build();

        var kubernetes = app.Services.GetRequiredService<IKubernetesService>();

        await app.StartAsync().DefaultTimeout();

        var suffix = app.Services.GetRequiredService<IOptions<DcpOptions>>().Value.ResourceNameSuffix;
        var redisContainer = await KubernetesHelper.GetResourceByNameMatchAsync<Container>(kubernetes, $"redis0-{ReplicaIdRegex}-{suffix}", r => r.Status?.EffectiveEnv is not null).DefaultTimeout();
        Assert.NotNull(redisContainer);

        var serviceA = await KubernetesHelper.GetResourceByNameAsync<Executable>(kubernetes, "servicea", suffix!, r => r.Status?.EffectiveEnv is not null).DefaultTimeout();
        Assert.NotNull(serviceA);

        var nodeApp = await KubernetesHelper.GetResourceByNameMatchAsync<Executable>(kubernetes, $"nodeapp-{ReplicaIdRegex}-{suffix}", r => r.Status?.EffectiveEnv is not null).DefaultTimeout();
        Assert.NotNull(nodeApp);

        Assert.Equal("redis:latest", redisContainer.Spec.Image);
        Assert.Equal("6379", GetEnv(redisContainer.Spec.Env, "REDIS_PORT"));
        Assert.Equal("6379", GetEnv(redisContainer.Status!.EffectiveEnv, "REDIS_PORT"));

        Assert.Equal($"{{{{- portForServing \"servicea-http0-{suffix}\" -}}}}", GetEnv(serviceA.Spec.Env, "PORT0"));
        var serviceAPortValue = GetEnv(serviceA.Status!.EffectiveEnv, "PORT0");
        Assert.False(string.IsNullOrEmpty(serviceAPortValue));
        Assert.NotEqual(0, int.Parse(serviceAPortValue, CultureInfo.InvariantCulture));

        Assert.Equal($"{{{{- portForServing \"nodeapp-{suffix}\" -}}}}", GetEnv(nodeApp.Spec.Env, "PORT"));
        var nodeAppPortValue = GetEnv(nodeApp.Status!.EffectiveEnv, "PORT");
        Assert.False(string.IsNullOrEmpty(nodeAppPortValue));
        Assert.NotEqual(0, int.Parse(nodeAppPortValue, CultureInfo.InvariantCulture));

        await app.StopAsync().DefaultTimeout();

        static string? GetEnv(IEnumerable<EnvVar>? envVars, string name)
        {
            Assert.NotNull(envVars);
            return Assert.Single(envVars.Where(e => e.Name == name)).Value;
        }
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
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

        await app.StartAsync().DefaultTimeout();

        var suffix = app.Services.GetRequiredService<IOptions<DcpOptions>>().Value.ResourceNameSuffix;
        var aspireDashboard = await KubernetesHelper.GetResourceByNameMatchAsync<Executable>(kubernetes, $"aspire-dashboard-{ReplicaIdRegex}-{suffix}", r => r.Status?.EffectiveEnv is not null).DefaultTimeout();
        Assert.NotNull(aspireDashboard);

        Assert.Equal("BrowserToken", GetEnv(aspireDashboard.Spec.Env, "DASHBOARD__FRONTEND__AUTHMODE"));
        Assert.Equal("ThisIsATestToken", GetEnv(aspireDashboard.Spec.Env, "DASHBOARD__FRONTEND__BROWSERTOKEN"));

        Assert.Equal("ApiKey", GetEnv(aspireDashboard.Spec.Env, "DASHBOARD__OTLP__AUTHMODE"));
        var keyBytes = Convert.FromHexString(GetEnv(aspireDashboard.Spec.Env, "DASHBOARD__OTLP__PRIMARYAPIKEY")!);
        Assert.Equal(16, keyBytes.Length);

        await app.StopAsync().DefaultTimeout();

        static string? GetEnv(IEnumerable<EnvVar>? envVars, string name)
        {
            Assert.NotNull(envVars);
            return Assert.Single(envVars.Where(e => e.Name == name)).Value;
        }
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
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

        await app.StartAsync().DefaultTimeout();

        var suffix = app.Services.GetRequiredService<IOptions<DcpOptions>>().Value.ResourceNameSuffix;
        var aspireDashboard = await KubernetesHelper.GetResourceByNameMatchAsync<Executable>(kubernetes, $"aspire-dashboard-{ReplicaIdRegex}-{suffix}", r => r.Status?.EffectiveEnv is not null).DefaultTimeout();
        Assert.NotNull(aspireDashboard);

        Assert.Equal("Unsecured", GetEnv(aspireDashboard.Spec.Env, "DASHBOARD__FRONTEND__AUTHMODE"));
        Assert.Equal("Unsecured", GetEnv(aspireDashboard.Spec.Env, "DASHBOARD__OTLP__AUTHMODE"));

        await app.StopAsync().DefaultTimeout();

        static string? GetEnv(IEnumerable<EnvVar>? envVars, string name)
        {
            Assert.NotNull(envVars);
            return Assert.Single(envVars.Where(e => e.Name == name)).Value;
        }
    }

    [Fact]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task VerifyDockerWithEntrypointWorks()
    {
        using var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.AddContainer("redis-cli", "redis")
            .WithEntrypoint("bob");

        await using var app = testProgram.Build();

        await app.StartAsync().DefaultTimeout();

        var s = app.Services.GetRequiredService<IKubernetesService>();

        var suffix = app.Services.GetRequiredService<IOptions<DcpOptions>>().Value.ResourceNameSuffix;
        var redisContainer = await KubernetesHelper.GetResourceByNameMatchAsync<Container>(s, $"redis-cli-{ReplicaIdRegex}-{suffix}",
            r => r.Status?.State == ContainerState.FailedToStart && (r.Status?.Message.Contains("bob") ?? false)).DefaultTimeout(TestConstants.LongTimeoutDuration);

        Assert.NotNull(redisContainer);
        Assert.Equal("redis:latest", redisContainer.Spec.Image);
        Assert.Equal("bob", redisContainer.Spec.Command);

        await app.StopAsync().DefaultTimeout();
    }

    [Fact]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task VerifyDockerWithBindMountWorksWithAbsolutePaths()
    {
        using var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        var sourcePath = Path.GetFullPath("/etc/path-here");
        testProgram.AppBuilder.AddContainer("redis-cli", "redis")
            .WithBindMount(sourcePath, "path-here");

        await using var app = testProgram.Build();

        await app.StartAsync().DefaultTimeout();

        var s = app.Services.GetRequiredService<IKubernetesService>();

        var suffix = app.Services.GetRequiredService<IOptions<DcpOptions>>().Value.ResourceNameSuffix;
        var redisContainer = await KubernetesHelper.GetResourceByNameMatchAsync<Container>(
                s,
                $"redis-cli-{ReplicaIdRegex}-{suffix}", r => r.Spec.VolumeMounts != null).DefaultTimeout();

        Assert.NotNull(redisContainer.Spec.VolumeMounts);
        Assert.NotEmpty(redisContainer.Spec.VolumeMounts);
        Assert.Equal(sourcePath, redisContainer.Spec.VolumeMounts[0].Source);

        await app.StopAsync().DefaultTimeout();
    }

    [Fact]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task VerifyDockerWithBindMountWorksWithRelativePaths()
    {
        using var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.AddContainer("redis-cli", "redis")
            .WithBindMount("etc/path-here", "path-here");

        await using var app = testProgram.Build();

        await app.StartAsync().DefaultTimeout();

        var s = app.Services.GetRequiredService<IKubernetesService>();

        var suffix = app.Services.GetRequiredService<IOptions<DcpOptions>>().Value.ResourceNameSuffix;
        var redisContainer = await KubernetesHelper.GetResourceByNameMatchAsync<Container>(
            s,
            $"redis-cli-{ReplicaIdRegex}-{suffix}", r => r.Spec.VolumeMounts != null).DefaultTimeout();

        Assert.NotNull(redisContainer.Spec.VolumeMounts);
        Assert.NotEmpty(redisContainer.Spec.VolumeMounts);
        Assert.NotEqual("etc/path-here", redisContainer.Spec.VolumeMounts[0].Source);
        Assert.True(Path.IsPathRooted(redisContainer.Spec.VolumeMounts[0].Source));

        await app.StopAsync().DefaultTimeout();
    }

    [Fact]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task VerifyDockerWithVolumeWorksWithName()
    {
        using var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        testProgram.AppBuilder.AddContainer("redis-cli", "redis")
            .WithVolume("test-volume-name", "/path-here");

        await using var app = testProgram.Build();

        await app.StartAsync().DefaultTimeout();

        var s = app.Services.GetRequiredService<IKubernetesService>();

        var suffix = app.Services.GetRequiredService<IOptions<DcpOptions>>().Value.ResourceNameSuffix;
        var redisContainer = await KubernetesHelper.GetResourceByNameMatchAsync<Container>(
                s,
                $"redis-cli-{ReplicaIdRegex}-{suffix}", r => r.Spec.VolumeMounts != null).DefaultTimeout();

        Assert.NotNull(redisContainer.Spec.VolumeMounts);
        Assert.NotEmpty(redisContainer.Spec.VolumeMounts);
        Assert.Equal("test-volume-name", redisContainer.Spec.VolumeMounts[0].Source);

        await app.StopAsync().DefaultTimeout();
    }

    [Fact]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task KubernetesHasResourceNameForContainersAndExes()
    {
        using var testProgram = CreateTestProgram(includeIntegrationServices: true);
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        await using var app = testProgram.Build();

        await app.StartAsync().DefaultTimeout();

        var s = app.Services.GetRequiredService<IKubernetesService>();

        var expectedExeResources = new HashSet<string>()
        {
            "servicea",
            "serviceb",
            "servicec",
            "workera",
            "integrationservicea"
        };

        var expectedContainerResources = new HashSet<string>()
        {
            "redis",
            "postgres"
        };

        await foreach (var resource in s.WatchAsync<Container>().DefaultTimeout())
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

        await foreach (var resource in s.WatchAsync<Executable>().DefaultTimeout())
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

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task ReplicasAndProxylessEndpointThrows()
    {
        using var testProgram = CreateTestProgram();
        testProgram.ServiceABuilder.WithReplicas(2).WithEndpoint("http", endpoint =>
        {
            endpoint.IsProxied = false;
        });
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        await using var app = testProgram.Build();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync()).DefaultTimeout();
        var suffix = app.Services.GetRequiredService<IOptions<DcpOptions>>().Value.ResourceNameSuffix;
        Assert.Equal($"Resource 'servicea-{suffix}' uses multiple replicas and a proxy-less endpoint 'http'. These features do not work together.", ex.Message);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task ProxylessEndpointWithoutPortThrows()
    {
        using var testProgram = CreateTestProgram();
        testProgram.ServiceABuilder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = null;
            endpoint.IsProxied = false;
        });
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        await using var app = testProgram.Build();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync()).DefaultTimeout();
        var suffix = app.Services.GetRequiredService<IOptions<DcpOptions>>().Value.ResourceNameSuffix;
        Assert.Equal($"Service 'servicea-{suffix}' needs to specify a port for endpoint 'http' since it isn't using a proxy.", ex.Message);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task ProxylessEndpointWorks()
    {
        using var testProgram = CreateTestProgram();

        testProgram.ServiceABuilder
            .WithEndpoint("http", e =>
            {
                e.Port = 1234;
                e.TargetPort = 1234;
                e.IsProxied = false;
            });
        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        await using var app = testProgram.Build();
        await app.StartAsync().DefaultTimeout();

        var client = app.CreateHttpClientWithResilience("servicea", "http");

        var result = await client.GetStringAsync("pid").DefaultTimeout(TestConstants.LongTimeoutDuration);
        Assert.NotNull(result);

        // Check that endpoint from launchsettings doesn't work
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using var client2 = new HttpClient(new SocketsHttpHandler
            {
                // Provide a timeout to avoid long timeout while trying to connect.
                ConnectTimeout = TimeSpan.FromSeconds(2)
            });
            await client2.GetStringAsync("http://localhost:5156/pid");
        }).DefaultTimeout();
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4599", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task ProxylessAndProxiedEndpointBothWorkOnSameResource()
    {
        using var testProgram = CreateTestProgram();

        testProgram.ServiceABuilder
            .WithEndpoint("http", e =>
            {
                e.Port = 1234;
                e.TargetPort = 1234;
                e.IsProxied = false;
            }, createIfNotExists: false)
            .WithEndpoint("https", e =>
            {
                e.UriScheme = "https";
                e.Port = 1543;
            }, createIfNotExists: true);

        testProgram.AppBuilder.Services.AddLogging(b => b.AddXunit(_testOutputHelper));

        await using var app = testProgram.Build();

        await app.StartAsync().DefaultTimeout();

        using var cts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.LongTimeoutDuration);
        var token = cts.Token;

        var urls = string.Empty;
        var httpEndPoint = app.GetEndpoint(testProgram.ServiceABuilder.Resource.Name, endpointName: "http");
        while (true)
        {
            try
            {
                using var client = new HttpClient();
                urls = await client.GetStringAsync($"{httpEndPoint}urls", token);
                break;
            }
            catch
            {
                await Task.Delay(100, token);
            }
        }

        Assert.Contains(httpEndPoint.ToString().Trim('/'), urls);

        // https endpoint is proxied so app won't have this specific endpoint
        var httpsEndpoint = app.GetEndpoint(testProgram.ServiceABuilder.Resource.Name, endpointName: "https");
        Assert.DoesNotContain(httpsEndpoint.ToString().Trim('/'), urls);

        while (true)
        {
            try
            {
                using var client = new HttpClient();
                var value = await client.GetStringAsync($"{httpsEndpoint}urls", token).DefaultTimeout();
                Assert.Equal(urls, value);
                break;
            }
            catch (Exception ex) when (ex is not EqualException)
            {
                await Task.Delay(100, token);
            }
        }
    }

    [Fact]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task ProxylessContainerCanBeReferenced()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var redis = builder.AddRedis("redis", 1234).WithEndpoint("tcp", endpoint =>
        {
            endpoint.IsProxied = false;
        });

        // Since port is not specified, this instance will use the container target port (6379) as the host port.
        var redisNoPort = builder.AddRedis("redisNoPort").WithEndpoint("tcp", endpoint =>
        {
            endpoint.IsProxied = false;
        });
        var servicea = builder.AddProject<Projects.ServiceA>("servicea")
            .WithReference(redis)
            .WithReference(redisNoPort);

        using var app = builder.Build();
        await app.StartAsync().DefaultTimeout();

        // Wait for the application to be ready
        await app.WaitForTextAsync("Application started.").DefaultTimeout();

        // Wait until the service itself starts.
        using var clientA = app.CreateHttpClient(servicea.Resource.Name, "http");
        await clientA.GetStringAsync("/").DefaultTimeout();

        var s = app.Services.GetRequiredService<IKubernetesService>();
        var exeList = await s.ListAsync<Executable>().DefaultTimeout();

        var suffix = app.Services.GetRequiredService<IOptions<DcpOptions>>().Value.ResourceNameSuffix;
        Assert.NotNull(suffix);
        var service = Assert.Single(exeList.Where(c => "servicea".Equals(c.AppModelResourceName) && c.Name().Contains(suffix)));
        var env = Assert.Single(service.Spec.Env!.Where(e => e.Name == "ConnectionStrings__redis"));
        Assert.Equal("localhost:1234", env.Value);

        var list = await s.ListAsync<Container>().DefaultTimeout();
        var redisContainer = Assert.Single(list.Where(c => Regex.IsMatch(c.Name(),$"redis-{ReplicaIdRegex}-{suffix}"))) ;
        Assert.Equal(1234, Assert.Single(redisContainer.Spec.Ports!).HostPort);

        var otherRedisEnv = Assert.Single(service.Spec.Env!.Where(e => e.Name == "ConnectionStrings__redisNoPort"));
        Assert.Equal("localhost:6379", otherRedisEnv.Value);

        var otherRedisContainer = Assert.Single(list.Where(c => Regex.IsMatch(c.Name(), $"redisNoPort-{ReplicaIdRegex}-{suffix}")));
        Assert.Equal(6379, Assert.Single(otherRedisContainer.Spec.Ports!).HostPort);

        await app.StopAsync().DefaultTimeout();
    }

    [Fact]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task ProxylessContainerWithoutPortThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var redis = builder.AddContainer("dummyRedis", "redis").WithEndpoint("tcp", endpoint =>
        {
            endpoint.IsProxied = false;
        });

        using var app = builder.Build();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync()).DefaultTimeout();
        var suffix = app.Services.GetRequiredService<IOptions<DcpOptions>>().Value.ResourceNameSuffix;
        Assert.Equal($"The endpoint 'tcp' for container resource 'dummyRedis-{suffix}' must specify the TargetPort value", ex.Message);
    }

    [Fact]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4651", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task AfterResourcesCreatedLifecycleHookWorks()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddRedis("redis");
        builder.Services.TryAddLifecycleHook<KubernetesTestLifecycleHook>();

        using var app = builder.Build();

        var s = app.Services.GetRequiredService<IKubernetesService>();
        var lifecycles = app.Services.GetServices<IDistributedApplicationLifecycleHook>();
        var kubernetesLifecycle = (KubernetesTestLifecycleHook)lifecycles.Where(l => l.GetType() == typeof(KubernetesTestLifecycleHook)).First();
        kubernetesLifecycle.KubernetesService = s;

        await app.StartAsync().DefaultTimeout();

        await kubernetesLifecycle.HooksCompleted.DefaultTimeout();
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

    private static TestProgram CreateTestProgram(
        string[]? args = null,
        bool includeIntegrationServices = false,
        bool disableDashboard = true,
        bool randomizePorts = true) =>
        TestProgram.Create<DistributedApplicationTests>(
            args,
            includeIntegrationServices: includeIntegrationServices,
            disableDashboard: disableDashboard,
            randomizePorts: randomizePorts);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Redis.Tests;

public class RedisFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyWaitForOnRedisBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        // We use the following check added to the Redis resource to block
        // dependent reosurces from starting. This means we'll have two checks
        // associated with the redis resource ... the built in one and the
        // one that we add here. We'll manipulate the TCS to allow us to check
        // states at various stages of the execution.
        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var redis = builder.AddRedis("redis")
                              .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddRedis("dependentresource")
                                       .WaitFor(redis); // Just using another redis instance as a dependent resource.

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        // What for the Redis server to start.
        await rns.WaitForResourceAsync(redis.Resource.Name, KnownResourceStates.Running, cts.Token);

        // Wait for the dependent resource to be in the Waiting state.
        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        // Now unblock the health check.
        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        // ... and wait for the resource as a whole to move into the health state.
        await rns.WaitForResourceAsync(redis.Resource.Name, (re => re.Snapshot.HealthStatus == HealthStatus.Healthy), cts.Token);

        // ... then the dependent resource should be able to move into a running state.
        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart; // Startup should now complete.

        // ... but we'll shut everything down immediately because we are done.
        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyRedisResource()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var redis = builder.AddRedis("redis");

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{redis.Resource.Name}"] = await redis.Resource.GetConnectionStringAsync()
        });

        hb.AddRedisClient(redis.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

        var db = redisClient.GetDatabase();

        await db.StringSetAsync("key", "value");

        var value = await db.StringGetAsync("key");

        Assert.Equal("value", value);
    }

    [Fact]
    [RequiresDocker]
    public async Task WithDataVolumeShouldPersistStateBetweenUsages()
    {
        // Use a volume to do a snapshot save

        using var builder1 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
        var redis1 = builder1.AddRedis("redis");

        // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
        var volumeName = VolumeNameGenerator.CreateVolumeName(redis1, nameof(WithDataVolumeShouldPersistStateBetweenUsages));
        redis1.WithDataVolume(volumeName);
        // if the volume already exists (because of a crashing previous run), delete it
        DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);

        using (var app = builder1.Build())
        {
            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            // BGSAVE is only available in admin mode, enable it for this instance
            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{redis1.Resource.Name}"] = $"{await redis1.Resource.GetConnectionStringAsync()},allowAdmin=true"
            });

            hb.AddRedisClient(redis1.Resource.Name);

            using (var host = hb.Build())
            {
                await host.StartAsync();

                var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

                var db = redisClient.GetDatabase();

                await db.StringSetAsync("key", "value");

                // Force Redis to save the keys (snapshotting)
                // c.f. https://redis.io/docs/latest/operate/oss_and_stack/management/persistence/

                await redisClient.GetServers().First().SaveAsync(SaveType.BackgroundSave);
            }

            // Stops the container, or the Volume would still be in use
            await app.StopAsync();
        }

        using var builder2 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
        var redis2 = builder2.AddRedis("redis").WithDataVolume(volumeName);

        using (var app = builder2.Build())
        {
            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{redis2.Resource.Name}"] = await redis2.Resource.GetConnectionStringAsync()
            });

            hb.AddRedisClient(redis2.Resource.Name);

            using (var host = hb.Build())
            {
                await host.StartAsync();

                var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

                var db = redisClient.GetDatabase();

                var value = await db.StringGetAsync("key");

                Assert.Equal("value", value);
            }

            // Stops the container, or the Volume would still be in use
            await app.StopAsync();
        }

        DockerUtils.AttemptDeleteDockerVolume(volumeName);
    }

    [Fact]
    [RequiresDocker]
    public async Task WithDataBindMountShouldPersistStateBetweenUsages()
    {
        var bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        if (!Directory.Exists(bindMountPath))
        {
            Directory.CreateDirectory(bindMountPath);
        }

        // Use a bind mount to do a snapshot save

        using var builder1 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
        var redis1 = builder1.AddRedis("redis").WithDataBindMount(bindMountPath);

        using (var app = builder1.Build())
        {
            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            // BGSAVE is only available in admin mode, enable it for this instance
            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{redis1.Resource.Name}"] = $"{await redis1.Resource.GetConnectionStringAsync()},allowAdmin=true"
            });

            hb.AddRedisClient(redis1.Resource.Name);

            using (var host = hb.Build())
            {
                await host.StartAsync();

                var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

                var db = redisClient.GetDatabase();

                await db.StringSetAsync("key", "value");

                // Force Redis to save the keys (snapshotting)
                // c.f. https://redis.io/docs/latest/operate/oss_and_stack/management/persistence/

                await redisClient.GetServers().First().SaveAsync(SaveType.BackgroundSave);
            }

            await app.StopAsync();
        }

        using var builder2 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
        var redis2 = builder2.AddRedis("redis").WithDataBindMount(bindMountPath);

        using (var app = builder2.Build())
        {
            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{redis2.Resource.Name}"] = await redis2.Resource.GetConnectionStringAsync()
            });

            hb.AddRedisClient(redis2.Resource.Name);

            using (var host = hb.Build())
            {
                await host.StartAsync();

                var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

                var db = redisClient.GetDatabase();

                var value = await db.StringGetAsync("key");

                Assert.Equal("value", value);
            }

            await app.StopAsync();
        }

        try
        {
            Directory.Delete(bindMountPath, recursive: true);
        }
        catch
        {
            // Don't fail test if we can't clean the temporary folder
        }
    }

    [Fact]
    [RequiresDocker]
    public async Task PersistenceIsDisabledByDefault()
    {
        // Checks that without enabling Redis Persistence the tests fail

        using var builder1 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
        var redis1 = builder1.AddRedis("redis");

        using (var app = builder1.Build())
        {
            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            // BGSAVE is only available in admin mode, enable it for this instance
            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{redis1.Resource.Name}"] = $"{await redis1.Resource.GetConnectionStringAsync()},allowAdmin=true"
            });

            hb.AddRedisClient(redis1.Resource.Name);

            using (var host = hb.Build())
            {
                await host.StartAsync();

                var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

                var db = redisClient.GetDatabase();

                await db.StringSetAsync("key", "value");
            }

            await app.StopAsync();
        }

        using var builder2 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
        var redis2 = builder2.AddRedis("redis");

        using (var app = builder2.Build())
        {
            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{redis2.Resource.Name}"] = await redis2.Resource.GetConnectionStringAsync()
            });

            hb.AddRedisClient(redis2.Resource.Name);

            using (var host = hb.Build())
            {
                await host.StartAsync();

                var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

                var db = redisClient.GetDatabase();

                var value = await db.StringGetAsync("key");

                Assert.True(value.IsNull);
            }

            await app.StopAsync();
        }
    }
}

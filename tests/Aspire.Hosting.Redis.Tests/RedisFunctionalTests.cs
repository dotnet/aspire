// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Json;
using System.Net;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;
using System.Text.Json.Nodes;

namespace Aspire.Hosting.Redis.Tests;

public class RedisFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyWaitForOnRedisBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddRedis("resource")
                           .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddRedis("dependentresource")
                                       .WaitFor(resource);

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        await rns.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await rns.WaitForResourceHealthyAsync(resource.Resource.Name, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyRedisCommanderResource()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        IResourceBuilder<RedisCommanderResource>? commanderBuilder = null;
        var redis = builder.AddRedis("redis").WithRedisCommander(c => commanderBuilder = c);
        Assert.NotNull(commanderBuilder);

        using var app = builder.Build();

        await app.StartAsync();

        await app.WaitForTextAsync("Redis Connection", resourceName: commanderBuilder.Resource.Name);

        var client = app.CreateHttpClient(commanderBuilder.Resource.Name, "http");

        var endpoint = redis.GetEndpoint("tcp");
        var path = $"/apiv2/server/R:{redis.Resource.Name}:{endpoint.TargetPort}:0/info";
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
    public async Task VerifyWithRedisInsightImportDatabases()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var redis1 = builder.AddRedis("redis-1");
        IResourceBuilder<RedisInsightResource>? redisInsightBuilder = null;
        var redis2 = builder.AddRedis("redis-2").WithRedisInsight(c => redisInsightBuilder = c);
        Assert.NotNull(redisInsightBuilder);

        // RedisInsight will import databases when it is ready, this task will run after the initial databases import
        // so we will use that to know when the databases have been successfully imported
        var redisInsightsReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        builder.Eventing.Subscribe<ResourceReadyEvent>(redisInsightBuilder.Resource, (evt, ct) =>
        {
            redisInsightsReady.TrySetResult();
            return Task.CompletedTask;
        });

        using var app = builder.Build();

        await app.StartAsync();

        await redisInsightsReady.Task.WaitAsync(cts.Token);

        var client = app.CreateHttpClient(redisInsightBuilder.Resource.Name, "http");

        var response = await client.GetAsync("/api/databases", cts.Token);
        response.EnsureSuccessStatusCode();

        var databases = await response.Content.ReadFromJsonAsync<List<RedisInsightDatabaseModel>>(cts.Token);

        Assert.NotNull(databases);
        Assert.Collection(databases,
        db =>
        {
            Assert.Equal(redis1.Resource.Name, db.Name);
            Assert.Equal(redis1.Resource.Name, db.Host);
            Assert.Equal(redis1.Resource.PrimaryEndpoint.TargetPort, db.Port);
            Assert.Equal("STANDALONE", db.ConnectionType);
            Assert.Equal(0, db.Db);
        },
        db =>
        {
            Assert.Equal(redis2.Resource.Name, db.Name);
            Assert.Equal(redis2.Resource.Name, db.Host);
            Assert.Equal(redis2.Resource.PrimaryEndpoint.TargetPort, db.Port);
            Assert.Equal("STANDALONE", db.ConnectionType);
            Assert.Equal(0, db.Db);
        });

        foreach (var db in databases)
        {
            var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var testConnectionResponse = await client.GetAsync($"/api/databases/test/{db.Id}", cts2.Token);
            response.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    [RequiresDocker]
    public async Task WithDataVolumeShouldPersistStateBetweenUsages()
    {
        // Use a volume to do a snapshot save

        using var builder1 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
        var redis1 = builder1.AddRedis("redis");

        // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
        var volumeName = VolumeNameGenerator.Generate(redis1, nameof(WithDataVolumeShouldPersistStateBetweenUsages));
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequiresDocker]
    public async Task RedisInsightWithDataShouldPersistStateBetweenUsages(bool useVolume)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

        string? volumeName = null;
        string? bindMountPath = null;

        try
        {
            using var builder1 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
            IResourceBuilder<RedisInsightResource>? redisInsightBuilder1 = null;
            var redis1 = builder1.AddRedis("redis")
                .WithRedisInsight(c => { redisInsightBuilder1 = c; });
            Assert.NotNull(redisInsightBuilder1);

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.Generate(redisInsightBuilder1, nameof(RedisInsightWithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);
                redisInsightBuilder1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                redisInsightBuilder1.WithDataBindMount(bindMountPath);
            }

            using (var app = builder1.Build())
            {
                await app.StartAsync();

                // RedisInsight will import databases when it is ready, this task will run after the initial databases import
                // so we will use that to know when the databases have been successfully imported
                var redisInsightsReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                builder1.Eventing.Subscribe<ResourceReadyEvent>(redisInsightBuilder1.Resource, (evt, ct) =>
                {
                    redisInsightsReady.TrySetResult();
                    return Task.CompletedTask;
                });

                await redisInsightsReady.Task.WaitAsync(cts.Token);

                try
                {
                    var httpClient = app.CreateHttpClient(redisInsightBuilder1.Resource.Name, "http");
                    await AcceptRedisInsightEula(httpClient, cts.Token);
                }
                finally
                {
                    // Stops the container, or the Volume would still be in use
                    await app.StopAsync();
                }
            }

            using var builder2 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

            IResourceBuilder<RedisInsightResource>? redisInsightBuilder2 = null;
            var redis2 = builder1.AddRedis("redis")
                .WithRedisInsight(c => { redisInsightBuilder2 = c; });
            Assert.NotNull(redisInsightBuilder2);

            if (useVolume)
            {
                redisInsightBuilder2.WithDataVolume(volumeName);
            }
            else
            {
                redisInsightBuilder2.WithDataBindMount(bindMountPath!);
            }

            using (var app = builder2.Build())
            {
                await app.StartAsync();

                // RedisInsight will import databases when it is ready, this task will run after the initial databases import
                // so we will use that to know when the databases have been successfully imported
                var redisInsightsReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                builder1.Eventing.Subscribe<ResourceReadyEvent>(redisInsightBuilder1.Resource, (evt, ct) =>
                {
                    redisInsightsReady.TrySetResult();
                    return Task.CompletedTask;
                });

                await redisInsightsReady.Task.WaitAsync(cts.Token);

                try
                {
                    var httpClient = app.CreateHttpClient(redisInsightBuilder2.Resource.Name, "http");
                    await EnsureRedisInsightEulaAccepted(httpClient, cts.Token);
                }
                finally
                {
                    // Stops the container, or the Volume would still be in use
                    await app.StopAsync();
                }
            }
        }
        finally
        {
            if (volumeName is not null)
            {
                DockerUtils.AttemptDeleteDockerVolume(volumeName);
            }

            if (bindMountPath is not null)
            {
                try
                {
                    Directory.Delete(bindMountPath, recursive: true);
                }
                catch
                {
                    // Don't fail test if we can't clean the temporary folder
                }
            }
        }
    }

    private static async Task EnsureRedisInsightEulaAccepted(HttpClient httpClient, CancellationToken ct)
    {
        var response = await httpClient.GetAsync("/api/settings", ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);

        var jo = JsonObject.Parse(content);
        Assert.NotNull(jo);
        var agreements = jo["agreements"];

        Assert.NotNull(agreements);
        Assert.False(agreements["analytics"]!.GetValue<bool>());
        Assert.False(agreements["notifications"]!.GetValue<bool>());
        Assert.False(agreements["encryption"]!.GetValue<bool>());
        Assert.True(agreements["eula"]!.GetValue<bool>());
    }

    static async Task AcceptRedisInsightEula(HttpClient client, CancellationToken ct)
    {
        var jsonContent = JsonContent.Create(new
        {
            agreements = new
            {
                eula = true,
                analytics = false,
                notifications = false,
                encryption = false,
            }
        });

        var apiUrl = $"/api/settings";

        var response = await client.PatchAsync(apiUrl, jsonContent, ct);

        response.EnsureSuccessStatusCode();

        await EnsureRedisInsightEulaAccepted(client, ct);
    }

    internal sealed class RedisInsightDatabaseModel
    {
        public string? Id { get; set; }
        public string? Host { get; set; }
        public int? Port { get; set; }
        public string? Name { get; set; }
        public int? Db { get; set; }
        public string? ConnectionType { get; set; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Json;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Redis.Tests;

public class RedisFunctionalTests(ITestOutputHelper testOutputHelper)
{
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
    public async Task VerifyWithRedisInsight()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var redis1 = builder.AddRedis("redis-1");
        var redis2 = builder.AddRedis("redis-2").WithRedisInsight();

        builder.Services.AddHttpClient();

        using var app = builder.Build();

        await app.StartAsync();

        var factory = app.Services.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient();

        var redisInsightResource = builder.Resources.OfType<RedisInsightResource>().Single();

        var insightEndpoint = redisInsightResource!.PrimaryEndpoint;

        var getDatabasesApiUrl = $"{insightEndpoint.Scheme}://{insightEndpoint.Host}:{insightEndpoint.Port}/api/databases";

        var pipeline = new ResiliencePipelineBuilder().AddRetry(new Polly.Retry.RetryStrategyOptions
        {
            Delay = TimeSpan.FromSeconds(2),
            MaxRetryAttempts = 10,
        }).Build();

        await pipeline.ExecuteAsync(async (ct) =>
        {
            var response = await client.GetAsync(getDatabasesApiUrl, ct)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var databases = await response.Content.ReadFromJsonAsync<List<RedisInsightDatabaseModel>>(ct);

            Assert.NotNull(databases);
            Assert.Collection(databases,
                    db =>
                    {
                        Assert.Equal(redis1.Resource.Name, db.Name);
                        Assert.Equal(redis1.Resource.PrimaryEndpoint.ContainerHost, db.Host);
                        Assert.Equal(redis1.Resource.PrimaryEndpoint.Port, db.Port);
                        Assert.Equal("STANDALONE", db.ConnectionType);
                        Assert.Equal(0, db.Db);
                    },
                    db =>
                    {
                        Assert.Equal(redis2.Resource.Name, db.Name);
                        Assert.Equal(redis2.Resource.PrimaryEndpoint.ContainerHost, db.Host);
                        Assert.Equal(redis2.Resource.PrimaryEndpoint.Port, db.Port);
                        Assert.Equal("STANDALONE", db.ConnectionType);
                        Assert.Equal(0, db.Db);
                    });

            foreach (var db in databases)
            {
                var testDatabaseConnectionApiUr = $"{insightEndpoint.Scheme}://{insightEndpoint.Host}:{insightEndpoint.Port}/api/databases/test/{db.Id}";
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var testConnectionResponse = await client.GetAsync(getDatabasesApiUrl, cts.Token)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }

        }, cts.Token).ConfigureAwait(false);
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

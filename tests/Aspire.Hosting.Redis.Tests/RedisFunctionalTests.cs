// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        var builder = CreateDistributedApplicationBuilder();

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

        var builder1 = CreateDistributedApplicationBuilder();
        var redis1 = builder1.AddRedis("redis");

        // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
        var volumeName = VolumeNameGenerator.CreateVolumeName(redis1, nameof(WithDataVolumeShouldPersistStateBetweenUsages));
        redis1.WithDataVolume(volumeName);

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

        var builder2 = CreateDistributedApplicationBuilder();
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

        var builder1 = CreateDistributedApplicationBuilder();
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

        var builder2 = CreateDistributedApplicationBuilder();
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
            File.Delete(bindMountPath);
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

        var builder1 = CreateDistributedApplicationBuilder();
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

        var builder2 = CreateDistributedApplicationBuilder();
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

    private TestDistributedApplicationBuilder CreateDistributedApplicationBuilder()
    {
        var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry();
        builder.Services.AddXunitLogging(testOutputHelper);
        return builder;
    }
}

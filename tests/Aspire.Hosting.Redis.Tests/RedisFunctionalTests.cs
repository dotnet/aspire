// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Xunit;
using Xunit.Sdk;

namespace Aspire.Hosting.Redis.Tests;

public class RedisFunctionalTests
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyRedisResource()
    {
        var builder = TestDistributedApplicationBuilder.Create();

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

    [Fact(Skip = "Debugging")]
    [RequiresDocker]
    public async Task WithDataVolumeShouldPersistStateBetweenUsages()
    {
        var volumeName = "myvolume1";

        // Use a volume to do a snapshot save

        await VerifyDataPersistence(
            options => options.WithDataVolume(volumeName),
            async redisClient => await redisClient.GetServers().First().SaveAsync(SaveType.BackgroundSave)
            );
    }

    [Fact(Skip = "Debugging")]
    [RequiresDocker]
    public async Task WithDataVolumeWithCustomPersistenceInterval()
    {
        var volumeName = "myvolume2";
        var snapshotInterval = TimeSpan.FromSeconds(1);

        // Use a volume to do a snapshot save with a custom interval

        await VerifyDataPersistence(
            options => options.WithDataVolume(volumeName).WithPersistence(snapshotInterval),
            async redisClient => await Task.Delay(snapshotInterval + TimeSpan.FromSeconds(1))
            );
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

        await VerifyDataPersistence(
            options => options.WithDataBindMount(bindMountPath),
            async redisClient => await redisClient.GetServers().First().SaveAsync(SaveType.BackgroundSave)
            );

        try
        {
            File.Delete(bindMountPath);
        }
        catch
        {
            // Don't fail test if we can't clean the temporary folder
        }
    }

    [Fact(Skip = "Debugging")]
    [RequiresDocker]
    public async Task WithDataBindMountWithCustomPersistenceInterval()
    {
        var bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        if (!Directory.Exists(bindMountPath))
        {
            Directory.CreateDirectory(bindMountPath);
        }

        var snapshotInterval = TimeSpan.FromSeconds(1);

        // Use a bind mount to do a snapshot save with a custom interval

        await VerifyDataPersistence(
            options => options.WithDataBindMount(bindMountPath).WithPersistence(snapshotInterval),
            async redisClient => await Task.Delay(snapshotInterval + TimeSpan.FromSeconds(1))
            );

        try
        {
            File.Delete(bindMountPath);
        }
        catch
        {
            // Don't fail test if we can't clean the temporary folder
        }
    }

    [Fact(Skip = "Debugging")]
    [RequiresDocker]
    public async Task PersistenceIsDisabledByDefault()
    {
        // Checks that without enabling Redis Persistence the tests fail

        await Assert.ThrowsAsync<EqualException>(async () =>
            await VerifyDataPersistence(
                options => { },
                redisClient => Task.CompletedTask
                )
            );
    }

    private static async Task VerifyDataPersistence(
        Action<IResourceBuilder<RedisResource>> configureResource,
        Func<IConnectionMultiplexer, Task> configureClient)
    {
        var builder1 = TestDistributedApplicationBuilder.Create();
        var redis1 = builder1.AddRedis("redis");

        configureResource?.Invoke(redis1);

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

                await configureClient.Invoke(redisClient);
            }
        }

        var builder2 = TestDistributedApplicationBuilder.Create();
        var redis2 = builder2.AddRedis("redis");
        configureResource?.Invoke(redis2);

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
        }
    }

}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Redis.Tests;

public class RedisStackFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyRedisStackResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1) })
            .Build();

        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var redisStack = builder.AddRedisStack("redis-stack");

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration[$"ConnectionStrings:{redisStack.Resource.Name}"] = await redisStack.Resource.GetConnectionStringAsync();

        hb.AddRedisClient(redisStack.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        await pipeline.ExecuteAsync(async token =>
        {
            var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

            var db = redisClient.GetDatabase();

            await db.StringSetAsync("key", "value");

            var value = await db.StringGetAsync("key");

            Assert.Equal("value", value);
        }, cts.Token);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker]
    public async Task WithDataShouldPersistStateBetweenUsages(bool useVolume)
    {

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(5) })
            .Build();

        string? volumeName = null;
        string? bindMountPath = null;

        try
        {
            using var builder1 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
            var redisStack1 = builder1.AddRedisStack("redis-stack");

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.CreateVolumeName(redisStack1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);
                redisStack1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                redisStack1.WithDataBindMount(bindMountPath);
            }

            using (var app = builder1.Build())
            {
                await app.StartAsync();

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    // BGSAVE is only available in admin mode, enable it for this instance
                    hb.Configuration[$"ConnectionStrings:{redisStack1.Resource.Name}"] = $"{await redisStack1.Resource.GetConnectionStringAsync()},allowAdmin=true";

                    hb.AddRedisClient(redisStack1.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(async token =>
                        {
                            var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

                            var db = redisClient.GetDatabase();

                            await db.StringSetAsync("key", "value");

                            // Force redis to save the keys (snapshotting)
                            // c.f. https://redis.io/docs/latest/operate/oss_and_stack/management/persistence/

                            await redisClient.GetServers().First().SaveAsync(SaveType.BackgroundSave);
                        }, cts.Token);

                    }
                }
                finally
                {
                    // Stops the container, or the Volume/mount would still be in use
                    await app.StopAsync();
                }
            }

            using var builder2 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
            var redisStack2 = builder2.AddRedisStack("redis-stack");
            if (useVolume)
            {
                redisStack2.WithDataVolume(volumeName);
            }
            else
            {
                redisStack2.WithDataBindMount(bindMountPath!);
            }
            using (var app = builder2.Build())
            {
                await app.StartAsync();

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration[$"ConnectionStrings:{redisStack2.Resource.Name}"] = await redisStack2.Resource.GetConnectionStringAsync();

                    hb.AddRedisClient(redisStack2.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(async token =>
                        {
                            var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

                            var db = redisClient.GetDatabase();

                            var value = await db.StringGetAsync("key");

                            Assert.Equal("value", value);
                        }, cts.Token);
                    }
                }
                finally
                {
                    // Stops the container, or the Volume/mount would still be in use
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
}

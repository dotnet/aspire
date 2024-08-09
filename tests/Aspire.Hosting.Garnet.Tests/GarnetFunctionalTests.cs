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

namespace Aspire.Hosting.Garnet.Tests;

public class GarnetFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyGarnetResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var pipeline = new ResiliencePipelineBuilder()
           .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(3) })
           .Build();

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

        var garnet = builder.AddGarnet("garnet");

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration[$"ConnectionStrings:{garnet.Resource.Name}"] = await garnet.Resource.ConnectionStringExpression.GetValueAsync(default);

        hb.AddRedisClient(garnet.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

        await pipeline.ExecuteAsync(async token =>
         {
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
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var pipeline = new ResiliencePipelineBuilder()
                            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(10) })
                            .Build();
        string? volumeName = null;
        string? bindMountPath = null;

        try
        {
            var builder1 = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);
            var garnet1 = builder1.AddGarnet("garnet");

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.CreateVolumeName(garnet1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), try to delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName);
                garnet1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                if (!Directory.Exists(bindMountPath))
                {
                    Directory.CreateDirectory(bindMountPath);
                }
                garnet1.WithDataBindMount(bindMountPath);
            }

            using (var app = builder1.Build())
            {
                await app.StartAsync();
                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration[$"ConnectionStrings:{garnet1.Resource.Name}"] = $"{await garnet1.Resource.ConnectionStringExpression.GetValueAsync(default)},allowAdmin = true";

                    hb.AddRedisClient("garnet");

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(async token =>
                        {
                            var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

                            var db = redisClient.GetDatabase();

                            await db.StringSetAsync("key", "value");
                            var value = await db.StringGetAsync("key");

                            Assert.Equal("value", value);
                            // Force Redis to save the keys (snapshotting)
                            // c.f. https://redis.io/docs/latest/operate/oss_and_stack/management/persistence/

#pragma warning disable CS0618 // Type or member is obsolete
                            await redisClient.GetServers().First().SaveAsync(SaveType.ForegroundSave);
#pragma warning restore CS0618 // Type or member is obsolete

                        }, cts.Token);
                    }
                }
                finally
                {
                    // Stops the container, or the Volume/mount would still be in use
                    await app.StopAsync();
                }
            }

            var builder2 = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);
            var garnet2 = builder2.AddGarnet("garnet");

            if (useVolume)
            {
                garnet2.WithDataVolume(volumeName);
            }
            else
            {
                garnet2.WithDataBindMount(bindMountPath!);
            }

            using (var app = builder2.Build())
            {
                await app.StartAsync();
                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration[$"ConnectionStrings:{garnet2.Resource.Name}"] = $"{await garnet2.Resource.ConnectionStringExpression.GetValueAsync(default)}";

                    hb.AddRedisClient("garnet");

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(async token =>
                        {
                            var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

                            var db = redisClient.GetDatabase();

                            var value = await db.StringGetAsync("key");

                            Assert.Equal("value", value);
                        });
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

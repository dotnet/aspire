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
using Aspire.Hosting.Tests.Dcp;
using System.Text.Json.Nodes;
using Aspire.Hosting;

namespace Aspire.Hosting.Redis.Tests;

public class RedisFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/7177")]
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

        await app.ResourceNotifications.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await app.ResourceNotifications.WaitForResourceHealthyAsync(resource.Resource.Name, cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

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
    [ActiveIssue("https://github.com/dotnet/aspire/issues/7291")]
    public async Task VerifyDatabasesAreNotDuplicatedForPersistentRedisInsightContainer()
    {
        var randomResourceSuffix = Random.Shared.Next(10000).ToString();
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        var configure = (DistributedApplicationOptions options) =>
        {
            options.ContainerRegistryOverride = ComponentTestConstants.AspireTestContainerRegistry;
        };

        using var builder1 = TestDistributedApplicationBuilder.Create(configure, testOutputHelper);
        builder1.Configuration[$"DcpPublisher:ResourceNameSuffix"] = randomResourceSuffix;

        IResourceBuilder<RedisInsightResource>? redisInsightBuilder = null;
        var redis1 = builder1.AddRedis("redisForInsightPersistence")
                .WithRedisInsight(c =>
                    {
                        redisInsightBuilder = c;
                        c.WithLifetime(ContainerLifetime.Persistent);
                    });

        // Wire up an additional event subcription to ResourceReadyEvent on the RedisInsightResource
        // instance. This works because the ResourceReadyEvent fires non-blocking sequential so the
        // wire-up that WithRedisInsight does is guaranteed to execute before this one does. So we then
        // use this to block pulling the list of databases until we know they've been updated. This
        // will repeated below for the second app.
        //
        // Issue: https://github.com/dotnet/aspire/issues/6455
        Assert.NotNull(redisInsightBuilder);
        var redisInsightsReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        builder1.Eventing.Subscribe<ResourceReadyEvent>(redisInsightBuilder.Resource, (evt, ct) =>
        {
            redisInsightsReady.TrySetResult();
            return Task.CompletedTask;
        });

        using var app1 = builder1.Build();

        await app1.StartAsync(cts.Token);

        await redisInsightsReady.Task.WaitAsync(cts.Token);

        using var client1 = app1.CreateHttpClient($"{redis1.Resource.Name}-insight", "http");
        var firstRunDatabases = await client1.GetFromJsonAsync<RedisInsightDatabaseModel[]>("/api/databases", cts.Token);

        Assert.NotNull(firstRunDatabases);
        Assert.Single(firstRunDatabases);
        Assert.Equal($"{redis1.Resource.Name}", firstRunDatabases[0].Name);

        await app1.StopAsync(cts.Token);

        using var builder2 = TestDistributedApplicationBuilder.Create(configure, testOutputHelper);
        builder2.Configuration[$"DcpPublisher:ResourceNameSuffix"] = randomResourceSuffix;

        var redis2 = builder2.AddRedis("redisForInsightPersistence")
                .WithRedisInsight(c =>
                {
                    redisInsightBuilder = c;
                    c.WithLifetime(ContainerLifetime.Persistent);
                });

        // Wire up an additional event subcription to ResourceReadyEvent on the RedisInsightResource
        // instance. This works because the ResourceReadyEvent fires non-blocking sequential so the
        // wire-up that WithRedisInsight does is guaranteed to execute before this one does. So we then
        // use this to block pulling the list of databases until we know they've been updated. This
        // will repeated below for the second app.
        Assert.NotNull(redisInsightBuilder);
        redisInsightsReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        builder2.Eventing.Subscribe<ResourceReadyEvent>(redisInsightBuilder.Resource, (evt, ct) =>
        {
            redisInsightsReady.TrySetResult();
            return Task.CompletedTask;
        });

        using var app2 = builder2.Build();
        await app2.StartAsync(cts.Token);

        await redisInsightsReady.Task.WaitAsync(cts.Token);

        using var client2 = app2.CreateHttpClient($"{redisInsightBuilder.Resource.Name}", "http");
        var secondRunDatabases = await client2.GetFromJsonAsync<RedisInsightDatabaseModel[]>("/api/databases", cts.Token);

        Assert.NotNull(secondRunDatabases);
        Assert.Single(secondRunDatabases);
        Assert.Equal($"{redis2.Resource.Name}", secondRunDatabases[0].Name);
        Assert.NotEqual(secondRunDatabases.Single().Id, firstRunDatabases.Single().Id);

        // HACK: This is a workaround for the fact that ApplicationExecutor is not a public type. What I have
        //       done here is I get the latest event from RNS for the insights instance which gives me the resource
        //       name as known from a DCP perspective. I then use the ApplicationExecutorProxy (introduced with this
        //       change to call the ApplicationExecutor stop method. The proxy is a public type with an internal
        //       constructor inside the Aspire.Hosting.Tests package. This is a short term solution for 9.0 to
        //       make sure that we have good test coverage for WithRedisInsight behavior, but we need a better
        //       long term solution in 9.x for folks that will want to do things like execute commands against
        //       resources to stop specific containers.
        var latestEvent = await app2.ResourceNotifications.WaitForResourceHealthyAsync(redisInsightBuilder.Resource.Name, cts.Token);
        var executorProxy = app2.Services.GetRequiredService<ApplicationOrchestratorProxy>();
        await executorProxy.StopResourceAsync(latestEvent.ResourceId, cts.Token);

        await app2.StopAsync(cts.Token);
    }

    [Fact]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/6099")]
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

        Directory.CreateDirectory(bindMountPath);

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
    [ActiveIssue("https://github.com/dotnet/aspire/issues/7176")]
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
            var redis2 = builder2.AddRedis("redis")
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
                builder2.Eventing.Subscribe<ResourceReadyEvent>(redisInsightBuilder2.Resource, (evt, ct) =>
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

    [Fact]
    [RequiresDocker]
    public async Task WithRedisCommanderShouldWorkWithPassword()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var passwordParameter = builder.AddParameter("pass", "p@ssw0rd1");

        var redis = builder.AddRedis("redis", password: passwordParameter)
           .WithRedisCommander();

        builder.Services.AddHttpClient();
        using var app = builder.Build();

        await app.StartAsync();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var redisCommander = Assert.Single(appModel.Resources.OfType<RedisCommanderResource>());

        await app.ResourceNotifications.WaitForResourceHealthyAsync(redis.Resource.Name, cts.Token);
        await app.ResourceNotifications.WaitForResourceHealthyAsync(redisCommander.Name, cts.Token);

        var endpoint = redisCommander.GetEndpoint("http");
        var redisCommanderUrl = endpoint.Url;
        Assert.NotNull(redisCommanderUrl);

        var clientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
        var client = clientFactory.CreateClient();

        var httpResponse = await client.GetAsync(redisCommanderUrl!);
        httpResponse.EnsureSuccessStatusCode();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Milvus.Client;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Milvus.Tests;

public class MilvusFunctionalTests(ITestOutputHelper testOutputHelper)
{
    private const string CollectionName = "book";

    [Fact]
    [RequiresDocker]
    public async Task VerifyMilvusResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var pipeline = new ResiliencePipelineBuilder()
           .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(3), ShouldHandle = new PredicateBuilder().Handle<RpcException>() })
           .Build();

        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var milvus = builder.AddMilvus("milvus");
        var db = milvus.AddDatabase("milvusdb", "db1");

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(default)
        });

        hb.AddMilvusClient(db.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        await pipeline.ExecuteAsync(
           async token =>
           {
               var milvusClient = host.Services.GetRequiredService<MilvusClient>();

               await milvusClient.CreateDatabaseAsync("db1", token);
               await CreateTestDataAsync(milvusClient, token);

           }, cts.Token);

    }

    private static async Task CreateTestDataAsync(MilvusClient milvusClient, CancellationToken token)
    {
        var collection = await milvusClient.CreateCollectionAsync(
                CollectionName,
                [
                    FieldSchema.Create<long>("book_id", isPrimaryKey:true),
                    FieldSchema.Create<long>("word_count"),
                    FieldSchema.CreateVarchar("book_name", 256),
                    FieldSchema.CreateFloatVector("book_intro", 2)
                ]
            , cancellationToken: token);

        var collections = await milvusClient.ListCollectionsAsync(cancellationToken: token);
        Assert.Single(collections, c => c.Name == CollectionName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequiresDocker]
    public async Task WithDataShouldPersistStateBetweenUsages(bool useVolume)
    {
        var dbname = "milvusdbtest";
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var pipeline = new ResiliencePipelineBuilder()
           .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(3), ShouldHandle = new PredicateBuilder().Handle<RpcException>() })
           .Build();

        string? volumeName = null;
        string? bindMountPath = null;

        try
        {
            using var builder1 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
            var milvus1 = builder1.AddMilvus("milvus1");
            var password = milvus1.Resource.ApiKeyParameter.Value;

            var db1 = milvus1.AddDatabase("milvusdb1", dbname);

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.CreateVolumeName(milvus1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);
                milvus1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                milvus1.WithDataBindMount(bindMountPath);
            }

            using (var app = builder1.Build())
            {
                await app.StartAsync();

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{db1.Resource.Name}"] = await db1.Resource.ConnectionStringExpression.GetValueAsync(default)
                    });

                    hb.AddMilvusClient(db1.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(
                           async token =>
                           {
                               var milvusClient = host.Services.GetRequiredService<MilvusClient>();

                               await milvusClient.CreateDatabaseAsync(dbname, token);
                               await CreateTestDataAsync(milvusClient, token);

                           }, cts.Token);

                    }
                }
                finally
                {
                    // Stops the container, or the Volume would still be in use
                    await app.StopAsync();
                }
            }

            using var builder2 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
            var passwordParameter = builder2.AddParameter("pwd");
            builder2.Configuration["Parameters:pwd"] = password;

            var milvus2 = builder2.AddMilvus("milvus2", passwordParameter);
            var db2 = milvus2.AddDatabase("milvusdb2", dbname);

            if (useVolume)
            {
                milvus2.WithDataVolume(volumeName);
            }
            else
            {
                milvus2.WithDataBindMount(bindMountPath!);
            }

            using (var app = builder2.Build())
            {
                await app.StartAsync();

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{db2.Resource.Name}"] = await db2.Resource.ConnectionStringExpression.GetValueAsync(default)
                    });

                    hb.AddMilvusClient(db2.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(
                           async token =>
                           {
                               var milvusClient = host.Services.GetRequiredService<MilvusClient>();

                               var collections = await milvusClient.ListCollectionsAsync(cancellationToken: token);

                               Assert.Single(collections, c => c.Name == CollectionName);

                           }, cts.Token);
                    }
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

    [Fact]
    [RequiresDocker]
    public async Task VerifyWaitForOnMilvusBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddMilvus("resource")
                              .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddMilvus("dependentresource")
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
    public async Task VerifyWaitForOnMilvusDatabaseBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddMilvus("resource")
                              .WithHealthCheck("blocking_check");

        var db = resource.AddDatabase("db");

        var dependentResource = builder.AddMilvus("dependentresource")
                                       .WaitFor(db);

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        await rns.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await rns.WaitForResourceAsync(db.Resource.Name, KnownResourceStates.Running, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await rns.WaitForResourceHealthyAsync(resource.Resource.Name, cts.Token);

        // Create the database.
        var connectionString = await resource.Resource.ConnectionStringExpression.GetValueAsync(cts.Token);
        var milvusClient = MilvusBuilderExtensions.CreateMilvusClient(app.Services, connectionString);
        await milvusClient.CreateDatabaseAsync(db.Resource.Name);

        await rns.WaitForResourceHealthyAsync(db.Resource.Name, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;

        await app.StopAsync();
    }

}

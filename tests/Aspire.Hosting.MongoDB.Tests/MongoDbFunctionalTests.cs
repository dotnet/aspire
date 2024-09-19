// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;
using Polly;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.MongoDB.Tests;

public class MongoDbFunctionalTests(ITestOutputHelper testOutputHelper)
{
    private const string CollectionName = "movie_collection";

    private static readonly Movie[] s_movies =
        [
            new() { Name = "The Shawshank Redemption"},
            new() { Name = "The Godfather"},
            new() { Name = "The Dark Knight"},
            new() { Name = "Schindler's List"},
        ];

    [Fact]
    [RequiresDocker]
    public async Task VerifyWaitForOnMongoBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddMongoDB("resource")
                           .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddMongoDB("dependentresource")
                                       .WaitFor(resource);

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        await rns.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await rns.WaitForResourceAsync(resource.Resource.Name, (re => re.Snapshot.HealthStatus == HealthStatus.Healthy), cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;
        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyMongoDBResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1) })
            .Build();

        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var mongodb = builder.AddMongoDB("mongodb");
        var db = mongodb.AddDatabase("testdb");
        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration[$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(default);

        hb.AddMongoDBClient(db.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        await pipeline.ExecuteAsync(async token =>
        {
            var mongoDatabase = host.Services.GetRequiredService<IMongoDatabase>();

            await CreateTestDataAsync(mongoDatabase, token);
        }, cts.Token);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker]
    public async Task WithDataShouldPersistStateBetweenUsages(bool useVolume)
    {
        var dbName = "testdb";
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(6));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1) })
            .Build();

        string? volumeName = null;
        string? bindMountPath = null;

        try
        {
            using var builder1 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
            var mongodb1 = builder1.AddMongoDB("mongodb");
            var db1 = mongodb1.AddDatabase(dbName);

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.CreateVolumeName(mongodb1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);
                mongodb1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                mongodb1.WithDataBindMount(bindMountPath);
            }

            using (var app = builder1.Build())
            {
                await app.StartAsync();
                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration[$"ConnectionStrings:{db1.Resource.Name}"] = await db1.Resource.ConnectionStringExpression.GetValueAsync(default);

                    hb.AddMongoDBClient(db1.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(async token =>
                        {
                            var mongoDatabase = host.Services.GetRequiredService<IMongoDatabase>();
                            await CreateTestDataAsync(mongoDatabase, token);
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
            var mongodb2 = builder2.AddMongoDB("mongodb");
            var db2 = mongodb2.AddDatabase(dbName);

            if (useVolume)
            {
                mongodb2.WithDataVolume(volumeName);
            }
            else
            {
                //mongodb shutdown has delay,so without delay to running instance using same data and second instance failed to start.
                await Task.Delay(TimeSpan.FromSeconds(10));
                mongodb2.WithDataBindMount(bindMountPath!);
            }

            using (var app = builder2.Build())
            {
                await app.StartAsync();
                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration[$"ConnectionStrings:{db2.Resource.Name}"] = await db2.Resource.ConnectionStringExpression.GetValueAsync(default);

                    hb.AddMongoDBClient(db2.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(async token =>
                        {
                            var mongoDatabase = host.Services.GetRequiredService<IMongoDatabase>();

                            var collection = mongoDatabase.GetCollection<Movie>(CollectionName);

                            var results = await collection.Find(new BsonDocument()).ToListAsync(token);

                            Assert.Collection(results,
                                            item => Assert.Contains("The Shawshank Redemption", item.Name),
                                            item => Assert.Contains("The Godfather", item.Name),
                                            item => Assert.Contains("The Dark Knight", item.Name),
                                            item => Assert.Contains("Schindler's List", item.Name)
                                            );
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

    [Fact]
    [RequiresDocker]
    public async Task VerifyWithInitBindMount()
    {
        // Creates a script that should be executed when the container is initialized.

        var dbName = "testdb";

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(6));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, BackoffType = DelayBackoffType.Linear, Delay = TimeSpan.FromSeconds(2) })
            .Build();

        var bindMountPath = Directory.CreateTempSubdirectory().FullName;

        if (!OperatingSystem.IsWindows())
        {
            // Change permissions for non-root accounts (container user account)
            const UnixFileMode OwnershipPermissions =
               UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
               UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
               UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

            File.SetUnixFileMode(bindMountPath, OwnershipPermissions);
        }

        try
        {
            var initFilePath = Path.Combine(bindMountPath, "mongo-init.js");
            await File.WriteAllTextAsync(initFilePath, $$"""
                db = db.getSiblingDB('{{dbName}}');

                db.createCollection('{{CollectionName}}');

                db.{{CollectionName}}.insertMany([
                    {
                        name: 'The Shawshank Redemption'
                    },
                    {
                        name: 'The Godfather'
                    },
                    {
                        name: 'The Dark Knight'
                    },
                    {
                        name: 'Schindler\'s List'
                    }
                ]);
            """);

            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(initFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
            }

            using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

            var mongodb = builder.AddMongoDB("mongodb")
                .WithInitBindMount(bindMountPath);

            var db = mongodb.AddDatabase(dbName);
            using var app = builder.Build();

            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration[$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(default);

            hb.AddMongoDBClient(db.Resource.Name);

            using var host = hb.Build();

            await host.StartAsync();

            var mongoDatabase = host.Services.GetRequiredService<IMongoDatabase>();

            await pipeline.ExecuteAsync(async token =>
            {
                var mongoDatabase = host.Services.GetRequiredService<IMongoDatabase>();

                var collection = mongoDatabase.GetCollection<Movie>(CollectionName);

                var results = await collection.Find(new BsonDocument()).ToListAsync(token);

                Assert.Collection(results,
                                item => Assert.Contains("The Shawshank Redemption", item.Name),
                                item => Assert.Contains("The Godfather", item.Name),
                                item => Assert.Contains("The Dark Knight", item.Name),
                                item => Assert.Contains("Schindler's List", item.Name)
                                );
            }, cts.Token);
        }
        finally
        {
            try
            {
                Directory.Delete(bindMountPath);
            }
            catch
            {
                // Don't fail test if we can't clean the temporary folder
            }
        }
    }

    private static async Task CreateTestDataAsync(IMongoDatabase mongoDatabase, CancellationToken token)
    {
        await mongoDatabase.CreateCollectionAsync(CollectionName, cancellationToken: token);
        var collection = mongoDatabase.GetCollection<Movie>(CollectionName);
        await collection.InsertManyAsync(s_movies, cancellationToken: token);

        var results = await collection.Find(new BsonDocument()).ToListAsync(token);

        Assert.Collection(results,
                        item => Assert.Contains("The Shawshank Redemption", item.Name),
                        item => Assert.Contains("The Godfather", item.Name),
                        item => Assert.Contains("The Dark Knight", item.Name),
                        item => Assert.Contains("Schindler's List", item.Name)
                        );
    }
}

public class Movie
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string? Name { get; set; }
}

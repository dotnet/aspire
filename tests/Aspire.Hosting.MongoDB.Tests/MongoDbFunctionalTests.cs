// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;
using Polly;

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
    public async Task VerifyMongoDBResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1) })
            .Build();

        var builder = CreateDistributedApplicationBuilder();

        var mongodb = builder.AddMongoDB("mongodb");
        var db = mongodb.AddDatabase("testdb");
        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(default)
        });

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
            var builder1 = CreateDistributedApplicationBuilder();
            var mongodb1 = builder1.AddMongoDB("mongodb");
            var db1 = mongodb1.AddDatabase(dbName);

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.CreateVolumeName(mongodb1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), try to delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName);
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

                    hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{db1.Resource.Name}"] = await db1.Resource.ConnectionStringExpression.GetValueAsync(default)
                    });

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

            var builder2 = CreateDistributedApplicationBuilder();
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

                    hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{db2.Resource.Name}"] = await db2.Resource.ConnectionStringExpression.GetValueAsync(default)
                    });

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
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1) })
            .Build();

        var bindMountPath = Directory.CreateTempSubdirectory().FullName;

        try
        {
            Directory.CreateDirectory(bindMountPath);

            File.WriteAllText(Path.Combine(bindMountPath, "mongo-init.js"), $$"""
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

            var builder = CreateDistributedApplicationBuilder();

            var mongodb = builder.AddMongoDB("mongodb");
            var db = mongodb.AddDatabase(dbName);
            using var app = builder.Build();

            mongodb.WithInitBindMount(bindMountPath);

            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(default)
            });

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

    private TestDistributedApplicationBuilder CreateDistributedApplicationBuilder()
    {
        var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry();
        builder.Services.AddXunitLogging(testOutputHelper);
        return builder;
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.MongoDB.Tests;

public class MongoDBFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyMongoDBResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = int.MaxValue, BackoffType = DelayBackoffType.Linear, Delay = TimeSpan.FromSeconds(2) })
            .Build();

        var builder = CreateDistributedApplicationBuilder();

        var databaseName = "db1";

        var db = builder.AddMongoDB("mongo").AddDatabase(databaseName);

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
            mongoDatabase.CreateCollection("movies", cancellationToken: token);
            var moviesCollection = mongoDatabase.GetCollection<Movie>("movies");
            await moviesCollection.InsertOneAsync(new Movie(ObjectId.GenerateNewId(), "Rocky I"), cancellationToken: token);
        }, cts.Token);

        var mongoDatabase = host.Services.GetRequiredService<IMongoDatabase>();
        var moviesCollection = mongoDatabase.GetCollection<Movie>("movies");
        var movies = await moviesCollection.Find(x => true).ToListAsync();

        Assert.Single(movies);
        Assert.Equal("Rocky I", movies[0].Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker]
    public async Task WithDataShouldPersistStateBetweenUsages(bool useVolume)
    {
        var databaseName = "db1";

        string? volumeName = null;
        string? bindMountPath = null;

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = int.MaxValue, BackoffType = DelayBackoffType.Linear, Delay = TimeSpan.FromSeconds(2) })
            .Build();

        try
        {
            var builder1 = CreateDistributedApplicationBuilder();

            var dbResource = builder1.AddMongoDB("mongo");
            var db1 = dbResource.AddDatabase(databaseName);

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.CreateVolumeName(dbResource, nameof(WithDataShouldPersistStateBetweenUsages));

                // If the volume already exists (because of a crashing previous run), try to delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName);
                dbResource.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Directory.CreateTempSubdirectory().FullName;
                dbResource.WithDataBindMount(bindMountPath);
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
                            mongoDatabase.CreateCollection("movies", cancellationToken: token);
                            var moviesCollection = mongoDatabase.GetCollection<Movie>("movies");
                            await moviesCollection.InsertOneAsync(new Movie(ObjectId.GenerateNewId(), "Rocky I"), cancellationToken: token);
                        }, cts.Token);

                        await app.StopAsync();

                        var healthCheckService = host.Services.GetRequiredService<HealthCheckService>();

                        // Wait until the container is actually.

                        while (true)
                        {
                            var health = await healthCheckService.CheckHealthAsync(cts.Token);
                            if (health.Status != HealthStatus.Healthy)
                            {
                                break;
                            }

                            cts.Token.ThrowIfCancellationRequested();
                        }
                    }
                }
                finally
                {
                    // Stops the container, or the Volume/mount would still be in use
                    await app.StopAsync();
                }
            }

            var builder2 = CreateDistributedApplicationBuilder();

            var dbResource2 = builder2.AddMongoDB("mongo");
            var db2 = dbResource2.AddDatabase(databaseName);

            if (useVolume)
            {
                dbResource2.WithDataVolume(volumeName);
            }
            else
            {
                dbResource2.WithDataBindMount(bindMountPath!);
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

                        await pipeline.ExecuteAsync(token =>
                        {
                            var mongoDatabase = host.Services.GetRequiredService<IMongoDatabase>();
                            mongoDatabase.GetCollection<Movie>("movies");
                            return ValueTask.CompletedTask;
                        }, cts.Token);

                        var mongoDatabase = host.Services.GetRequiredService<IMongoDatabase>();
                        var moviesCollection = mongoDatabase.GetCollection<Movie>("movies");
                        var movies = await moviesCollection.Find(x => true).ToListAsync();
                        Assert.Single(movies);
                        Assert.Equal("Rocky I", movies[0].Name);
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
                    Directory.Delete(bindMountPath);
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

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = int.MaxValue, BackoffType = DelayBackoffType.Linear, Delay = TimeSpan.FromSeconds(2) })
            .Build();

        var bindMountPath = Directory.CreateTempSubdirectory().FullName;

        try
        {
            Directory.CreateDirectory(bindMountPath);

            File.WriteAllText(Path.Combine(bindMountPath, "mongo-init.js"), """
                db = db.getSiblingDB('db1');

                db.createCollection('movies');

                db.movies.insertMany([
                 {
                    Name: 'Rocky I'
                  } 
                ]);
            """);

            var builder = CreateDistributedApplicationBuilder();

            var databaseName = "db1";

            var dbResource = builder.AddMongoDB("mongo");
            var db = dbResource.AddDatabase(databaseName);

            dbResource.WithInitBindMount(bindMountPath);

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

            var mongoDatabase = host.Services.GetRequiredService<IMongoDatabase>();

            // Wait until the database is available
            await pipeline.ExecuteAsync(async token =>
            {
                var collections = await mongoDatabase.ListCollectionsAsync(cancellationToken: token);
                Assert.True(collections.Any(token));
            }, cts.Token);

            var moviesCollection = mongoDatabase.GetCollection<Movie>("movies");
            var movies = await moviesCollection.Find(x => true).ToListAsync();
            Assert.Single(movies);
            Assert.Equal("Rocky I", movies[0].Name);
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

    private TestDistributedApplicationBuilder CreateDistributedApplicationBuilder()
    {
        var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry();
        builder.Services.AddXunitLogging(testOutputHelper);
        builder.Services.AddHostedService<ResourceLoggerForwarderService>();
        return builder;
    }
}

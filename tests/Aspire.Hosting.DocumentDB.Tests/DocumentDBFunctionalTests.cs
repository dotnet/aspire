// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Polly;

namespace Aspire.Hosting.DocumentDB.Tests;

public class DocumentDBFunctionalTests(ITestOutputHelper testOutputHelper)
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
    public async Task VerifyDocumentDBResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1) })
            .Build();

        //using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
        using var builder = TestDistributedApplicationBuilder.Create(options => { }, testOutputHelper);

        var DocumentDB = builder
            .AddDocumentDB("DocumentDB", tls: true, allowInsecureTls: true)
            .WithEndpoint(port: 10260, targetPort: 10260, name: "ssms", isExternal: true);

        var db = DocumentDB.AddDatabase("testdb");

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        var connStr = await db.Resource.ConnectionStringExpression.GetValueAsync(default);

        hb.Configuration[$"ConnectionStrings:{db.Resource.Name}"] = connStr;

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
    //[InlineData(true)]
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
            var DocumentDB1 = builder1.AddDocumentDB("DocumentDB", tls: true, allowInsecureTls: true);
            var password = DocumentDB1.Resource.PasswordParameter!.Value;
            var db1 = DocumentDB1.AddDatabase(dbName);

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.Generate(DocumentDB1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);
                DocumentDB1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                Directory.CreateDirectory(bindMountPath);

                if (!OperatingSystem.IsWindows())
                {
                    // PostgreSQL requires strict permissions on its data directory
                    // Set permissions to 0700 (user read/write/execute only) as required by PostgreSQL
                    const UnixFileMode BindMountPermissions =
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

                    File.SetUnixFileMode(bindMountPath, BindMountPermissions);

                    Console.WriteLine($"Created bind mount path: {bindMountPath} with permissions {BindMountPermissions}");
                }
                else
                {
                    Console.WriteLine($"Created bind mount path: {bindMountPath}");
                }

                DocumentDB1.WithDataBindMount(bindMountPath);
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
            var passwordParameter2 = builder2.AddParameter("pwd", password);

            var DocumentDB2 = builder2.AddDocumentDB("DocumentDB", password: passwordParameter2, tls: true, allowInsecureTls: true);
            var db2 = DocumentDB2.AddDatabase(dbName);

            if (useVolume)
            {
                DocumentDB2.WithDataVolume(volumeName);
            }
            else
            {
                DocumentDB2.WithDataBindMount(bindMountPath!);
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

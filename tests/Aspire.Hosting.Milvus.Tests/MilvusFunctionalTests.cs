// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Milvus.Client;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Milvus.Tests;

public class MilvusFunctionalTests(ITestOutputHelper testOutputHelper)
{
    // Right now can not set user and password for super user of Milvus at startup. default user and password is root:Milvus.
    // https://github.com/milvus-io/milvus/issues/33058
    private const string MilvusToken = "root:Milvus";

    [Fact]
    [RequiresDocker]
    public async Task VerifyMilvusResource()
    {
        var builder = CreateDistributedApplicationBuilder();

        builder.Configuration["Parameters:apikey"] = MilvusToken;
        var apiKey = builder.AddParameter("apikey");
        var milvus = builder.AddMilvus("milvus", apiKey: apiKey);

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{milvus.Resource.Name}"] = await milvus.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
        });

        hb.AddMilvusClient(milvus.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        var milvusClient = host.Services.GetRequiredService<MilvusClient>();

        string collectionName = "book";
        var collection = await milvusClient.CreateCollectionAsync(
                collectionName,
                new[] {
                FieldSchema.Create<long>("book_id", isPrimaryKey:true),
                FieldSchema.Create<long>("word_count"),
                FieldSchema.CreateVarchar("book_name", 256),
                FieldSchema.CreateFloatVector("book_intro", 2)
                }
            );

        var collections = await milvusClient.ListCollectionsAsync();

        Assert.Single(collections, c => c.Name == collectionName);
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyMilvusDatabaseResource()
    {
        var builder = CreateDistributedApplicationBuilder();

        builder.Configuration["Parameters:apikey"] = MilvusToken;
        var apiKey = builder.AddParameter("apikey");
        var milvus = builder.AddMilvus("milvus", apiKey: apiKey);
        var db = milvus.AddDatabase("milvusdb", "db1");

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
        });

        hb.AddMilvusClient(db.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        var milvusClient = host.Services.GetRequiredService<MilvusClient>();
        await milvusClient.CreateDatabaseAsync("db1");
        string collectionName = "book";
        var collection = await milvusClient.CreateCollectionAsync(
                collectionName,
                new[] {
                FieldSchema.Create<long>("book_id", isPrimaryKey:true),
                FieldSchema.Create<long>("word_count"),
                FieldSchema.CreateVarchar("book_name", 256),
                FieldSchema.CreateFloatVector("book_intro", 2)
                }
            );

        var collections = await milvusClient.ListCollectionsAsync();

        Assert.Single(collections, c => c.Name == collectionName);
    }

    [Fact]
    [RequiresDocker]
    public async Task WithDataVolumeShouldPersistStateBetweenUsages()
    {
        var builder1 = CreateDistributedApplicationBuilder();
        builder1.Configuration["Parameters:apikey"] = MilvusToken;
        var apiKey1 = builder1.AddParameter("apikey");
        var milvus1 = builder1.AddMilvus("milvus", apiKey1);

        // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
        var volumeName = VolumeNameGenerator.CreateVolumeName(milvus1, nameof(WithDataVolumeShouldPersistStateBetweenUsages));
        milvus1.WithDataVolume(volumeName);
        string collectionName = "book";

        using (var app = builder1.Build())
        {
            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{milvus1.Resource.Name}"] = await milvus1.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
            });

            hb.AddMilvusClient(milvus1.Resource.Name);

            using (var host = hb.Build())
            {
                await host.StartAsync();

                var milvusClient = host.Services.GetRequiredService<MilvusClient>();

                var collection = await milvusClient.CreateCollectionAsync(
                        collectionName,
                        new[] {
                FieldSchema.Create<long>("book_id", isPrimaryKey:true),
                FieldSchema.Create<long>("word_count"),
                FieldSchema.CreateVarchar("book_name", 256),
                FieldSchema.CreateFloatVector("book_intro", 2)
                        });
            }

            // Stops the container, or the Volume would still be in use
            await app.StopAsync();
        }

        var builder2 = CreateDistributedApplicationBuilder();
        builder2.Configuration["Parameters:apikey"] = MilvusToken;
        var apiKey2 = builder2.AddParameter("apikey");
        var milvus2 = builder2.AddMilvus("milvus", apiKey2).WithDataVolume(volumeName);

        using (var app = builder2.Build())
        {
            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{milvus2.Resource.Name}"] = await milvus2.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
            });

            hb.AddMilvusClient(milvus2.Resource.Name);

            using (var host = hb.Build())
            {
                await host.StartAsync();

                var milvusClient = host.Services.GetRequiredService<MilvusClient>();

                var collections = await milvusClient.ListCollectionsAsync();

                Assert.Single(collections, c => c.Name == collectionName);
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

        var builder1 = CreateDistributedApplicationBuilder();
        builder1.Configuration["Parameters:apikey"] = MilvusToken;
        var apiKey1 = builder1.AddParameter("apikey");
        var milvus1 = builder1.AddMilvus("milvus", apiKey1).WithDataBindMount(bindMountPath);

        string collectionName = "book";

        using (var app = builder1.Build())
        {
            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{milvus1.Resource.Name}"] = await milvus1.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
            });

            hb.AddMilvusClient(milvus1.Resource.Name);

            using (var host = hb.Build())
            {
                await host.StartAsync();

                var milvusClient = host.Services.GetRequiredService<MilvusClient>();
                var collection = await milvusClient.CreateCollectionAsync(
                       collectionName,
                       new[] {
                FieldSchema.Create<long>("book_id", isPrimaryKey:true),
                FieldSchema.Create<long>("word_count"),
                FieldSchema.CreateVarchar("book_name", 256),
                FieldSchema.CreateFloatVector("book_intro", 2)
                       });
            }

            await app.StopAsync();
        }

        var builder2 = CreateDistributedApplicationBuilder();
        builder2.Configuration["Parameters:apikey"] = MilvusToken;
        var apiKey2 = builder2.AddParameter("apikey");
        var milvus2 = builder2.AddMilvus("milvus2", apiKey2).WithDataBindMount(bindMountPath);

        using (var app = builder2.Build())
        {
            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{milvus2.Resource.Name}"] = await milvus2.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
            });

            hb.AddMilvusClient(milvus2.Resource.Name);

            using (var host = hb.Build())
            {
                await host.StartAsync();

                var milvusClient = host.Services.GetRequiredService<MilvusClient>();

                var collections = await milvusClient.ListCollectionsAsync();

                Assert.Single(collections, c => c.Name == collectionName);
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
        var builder1 = CreateDistributedApplicationBuilder();
        builder1.Configuration["Parameters:apikey"] = MilvusToken;
        var apiKey1 = builder1.AddParameter("apikey");
        var milvus1 = builder1.AddMilvus("milvus", apiKey1);

        using (var app = builder1.Build())
        {
            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{milvus1.Resource.Name}"] = await milvus1.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
            });

            hb.AddMilvusClient(milvus1.Resource.Name);

            using (var host = hb.Build())
            {
                await host.StartAsync();

                var milvusClient = host.Services.GetRequiredService<MilvusClient>();

                string collectionName = "book";
                var collection = await milvusClient.CreateCollectionAsync(
                        collectionName,
                        new[] {
                FieldSchema.Create<long>("book_id", isPrimaryKey:true),
                FieldSchema.Create<long>("word_count"),
                FieldSchema.CreateVarchar("book_name", 256),
                FieldSchema.CreateFloatVector("book_intro", 2)
                        });
            }

            await app.StopAsync();
        }

        var builder2 = CreateDistributedApplicationBuilder();
        builder2.Configuration["Parameters:apikey"] = MilvusToken;
        var apiKey2 = builder2.AddParameter("apikey");
        var milvus2 = builder2.AddMilvus("milvus", apiKey2);

        using (var app = builder2.Build())
        {
            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{milvus2.Resource.Name}"] = await milvus2.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
            });

            hb.AddMilvusClient(milvus2.Resource.Name);

            using (var host = hb.Build())
            {
                await host.StartAsync();

                var milvusClient = host.Services.GetRequiredService<MilvusClient>();

                var collections = await milvusClient.ListCollectionsAsync();

                Assert.Empty(collections);
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

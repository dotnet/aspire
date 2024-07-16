// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Qdrant.Tests;

public class QdrantFunctionalTests(ITestOutputHelper testOutputHelper)
{
    private const string CollectionName = "test_collection";
    private static readonly float[] s_testVector = { 0.10022575f, -0.23998135f };

    [Fact]
    [RequiresDocker]
    public async Task VerifyQdrantResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<RpcException>() })
            .Build();

        var builder = CreateDistributedApplicationBuilder();

        var qdrant = builder.AddQdrant("qdrant");

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{qdrant.Resource.Name}"] = await qdrant.Resource.ConnectionStringExpression.GetValueAsync(default)
        });

        hb.AddQdrantClient(qdrant.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        await pipeline.ExecuteAsync(async token =>
        {
            var qdrantClient = host.Services.GetRequiredService<QdrantClient>();

            await CreateTestDataAsync(qdrantClient, token);

            var results = await qdrantClient.SearchAsync(CollectionName, s_testVector, limit: 1, cancellationToken: token);
            Assert.Collection(results,
                r => Assert.Equal("Test", r.Payload["title"].StringValue));
        }, cts.Token);
    }

    private static async Task CreateTestDataAsync(QdrantClient qdrantClient, CancellationToken cancellationToken)
    {
        await qdrantClient.CreateCollectionAsync(CollectionName, new VectorParams { Size = 2, Distance = Distance.Cosine }, cancellationToken: cancellationToken);

        var data = new[]
        {
            new PointStruct
            {
                Id = 1,
                Vectors = s_testVector,
                Payload =
                {
                    ["title"] = "Test"
                }
            }
        };
        var updateResult = await qdrantClient.UpsertAsync(CollectionName, data, cancellationToken: cancellationToken);
        Assert.Equal(UpdateStatus.Completed, updateResult.Status);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker]
    public async Task WithDataShouldPersistStateBetweenUsages(bool useVolume)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<RpcException>() })
            .Build();

        string? volumeName = null;
        string? bindMountPath = null;

        try
        {
            var builder1 = CreateDistributedApplicationBuilder();
            var qdrant1 = builder1.AddQdrant("qdrant");

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.CreateVolumeName(qdrant1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), try to delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName);
                qdrant1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                qdrant1.WithDataBindMount(bindMountPath);
            }

            using (var app = builder1.Build())
            {
                await app.StartAsync();
                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{qdrant1.Resource.Name}"] = await qdrant1.Resource.ConnectionStringExpression.GetValueAsync(default)
                    });

                    hb.AddQdrantClient(qdrant1.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(async token =>
                        {
                            var qdrantClient = host.Services.GetRequiredService<QdrantClient>();

                            await CreateTestDataAsync(qdrantClient, token);
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
            var qdrant2 = builder2.AddQdrant("qdrant");

            if (useVolume)
            {
                qdrant2.WithDataVolume(volumeName);
            }
            else
            {
                qdrant2.WithDataBindMount(bindMountPath!);
            }

            using (var app = builder2.Build())
            {
                await app.StartAsync();
                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{qdrant2.Resource.Name}"] = await qdrant2.Resource.ConnectionStringExpression.GetValueAsync(default)
                    });

                    hb.AddQdrantClient(qdrant2.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(async token =>
                        {
                            var qdrantClient = host.Services.GetRequiredService<QdrantClient>();

                            var results = await qdrantClient.SearchAsync(CollectionName, s_testVector, limit: 1, cancellationToken: token);
                            Assert.Collection(results,
                                r => Assert.Equal("Test", r.Payload["title"].StringValue));
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
                    File.Delete(bindMountPath);
                }
                catch
                {
                    // Don't fail test if we can't clean the temporary folder
                }
            }
        }
    }

    private TestDistributedApplicationBuilder CreateDistributedApplicationBuilder()
    {
        var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry();
        builder.Services.AddXunitLogging(testOutputHelper);
        return builder;
    }
}

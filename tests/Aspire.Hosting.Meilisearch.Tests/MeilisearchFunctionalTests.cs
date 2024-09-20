// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Meilisearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Meilisearch.Tests;

public class MeilisearchFunctionalTests(ITestOutputHelper testOutputHelper)
{
    private const string IndexName = "movies";
    private static readonly Movie[] s_data = [
                new() { Id = "1", Title = "Carol", Genres = ["Romance", "Drama"] },
                new() { Id = "2", Title = "Wonder Woman", Genres = ["Action", "Adventure"] },
                new() { Id = "3", Title = "Life of Pi", Genres = ["Adventure", "Drama"] },
                new() { Id = "4", Title = "Mad Max: Fury Road", Genres = ["Adventure", "Science Fiction"] },
                new() { Id = "5", Title = "Moana", Genres = ["Fantasy", "Action"] },
                new() { Id = "6", Title = "Philadelphia", Genres = ["Drama"] }
            ];
    [Fact]
    [RequiresDocker]
    public async Task VerifyMeilisearchResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var pipeline = new ResiliencePipelineBuilder()
           .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(10) })
           .Build();

        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var meilisearch = builder.AddMeilisearch("meilisearch");

        using var app = builder.Build();

        await app.StartAsync();

        await app.WaitForTextAsync("Server listening", meilisearch.Resource.Name);

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration[$"ConnectionStrings:{meilisearch.Resource.Name}"] = await meilisearch.Resource.ConnectionStringExpression.GetValueAsync(default);

        hb.AddMeilisearchClient(meilisearch.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        await pipeline.ExecuteAsync(
            async token =>
            {

                var meilisearchClient = host.Services.GetRequiredService<MeilisearchClient>();

                await CreateTestData(meilisearchClient);
            }, cts.Token);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker]
    public async Task WithDataShouldPersistStateBetweenUsages(bool useVolume)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var pipeline = new ResiliencePipelineBuilder()
           .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(10) })
           .Build();

        string? volumeName = null;
        string? bindMountPath = null;

        try
        {
            using var builder1 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

            var meilisearch1 = builder1.AddMeilisearch("meilisearch");

            var masterKey = meilisearch1.Resource.MasterKeyParameter.Value;

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.CreateVolumeName(meilisearch1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);
                meilisearch1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Directory.CreateTempSubdirectory().FullName;
                meilisearch1.WithDataBindMount(bindMountPath);
            }

            using (var app = builder1.Build())
            {
                await app.StartAsync();

                await app.WaitForTextAsync("Server listening", meilisearch1.Resource.Name);

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration[$"ConnectionStrings:{meilisearch1.Resource.Name}"] = await meilisearch1.Resource.ConnectionStringExpression.GetValueAsync(default);

                    hb.AddMeilisearchClient(meilisearch1.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(
                            async token =>
                            {
                                var meilisearchClient = host.Services.GetRequiredService<MeilisearchClient>();
                                await CreateTestData(meilisearchClient);
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
            var masterkeyParameter2 = builder2.AddParameter("masterkey");
            builder2.Configuration["Parameters:masterkey"] = masterKey;
            var meilisearch2 = builder2.AddMeilisearch("meilisearch", masterkeyParameter2);

            if (useVolume)
            {
                meilisearch2.WithDataVolume(volumeName);
            }
            else
            {
                meilisearch2.WithDataBindMount(bindMountPath!);
            }

            using (var app = builder2.Build())
            {
                await app.StartAsync();

                await app.WaitForTextAsync("Server listening", meilisearch2.Resource.Name);

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration[$"ConnectionStrings:{meilisearch2.Resource.Name}"] = await meilisearch2.Resource.ConnectionStringExpression.GetValueAsync(default);

                    hb.AddMeilisearchClient(meilisearch2.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();
                        await pipeline.ExecuteAsync(
                            async token =>
                            {
                                var meilisearchClient = host.Services.GetRequiredService<MeilisearchClient>();
                                var index = meilisearchClient.Index(IndexName);
                                var document = await index.GetDocumentAsync<Movie>("1", cancellationToken: token);
                                Assert.NotNull(document);
                                Assert.Equal(document.Title, s_data[0].Title);
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
    public async Task VerifyWaitForOnMeilisearchBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddMeilisearch("resource")
                              .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddMeilisearch("dependentresource")
                                       .WaitFor(resource);

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        await rns.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await rns.WaitForResourceAsync(resource.Resource.Name, (re => re.Snapshot.HealthStatus == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy), cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;

        await app.StopAsync();
    }

    private static async Task CreateTestData(MeilisearchClient meilisearchClient)
    {
        var index = meilisearchClient.Index(IndexName);

        // If the index 'movies' does not exist, Meilisearch creates it when you first add the documents.
        var task = await index.AddDocumentsAsync<Movie>(s_data);
        Assert.NotNull(task);

        var response = await index.WaitForTaskAsync(task.TaskUid);
        Assert.NotNull(response);

        Assert.Equal(TaskInfoStatus.Succeeded, response.Status);

        var document = await index.GetDocumentAsync<Movie>("1");
        Assert.NotNull(document);
        Assert.Equal(document.Title, s_data[0].Title);
    }

    private sealed class Movie
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public IEnumerable<string>? Genres { get; set; }
    }
}

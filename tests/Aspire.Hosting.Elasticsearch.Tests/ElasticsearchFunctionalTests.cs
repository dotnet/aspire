// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Xunit;

namespace Aspire.Hosting.Elasticsearch.Tests;

public class ElasticsearchFunctionalTests
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyElasticsearchResource()
    {
        var builder = CreateDistributedApplicationBuilder();

        var elasticsearch = builder.AddElasticsearch("elasticsearch");

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{elasticsearch.Resource.Name}"] = await elasticsearch.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
        });

        hb.AddElasticsearchClient(elasticsearch.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        var elasticsearchClient = host.Services.GetRequiredService<ElasticsearchClient>();

        var person = new Person
        {
            Id = 1,
            FirstName = "Alireza",
            LastName = "Baloochi"
        };

        var pipeline = new ResiliencePipelineBuilder()
           .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(3) })
           .AddTimeout(TimeSpan.FromSeconds(5))
           .Build();

        await pipeline.ExecuteAsync(
            async token =>
            {
                var indexResponse = await elasticsearchClient.IndexAsync<Person>(person, "people", "1",CancellationToken.None);

                var getResponse = await elasticsearchClient.GetAsync<Person>("people", "1", CancellationToken.None);

                Assert.True(indexResponse.IsSuccess());
                Assert.True(getResponse.IsSuccess());
                Assert.NotNull(getResponse.Source);
                Assert.Equal(person.Id, getResponse.Source?.Id);
            });
    }

    [Fact]
    [SkipOnCI("https://github.com/dotnet/aspire/issues/4968")]
    [RequiresDocker]
    public async Task WithDataVolumeShouldPersistStateBetweenUsages()
    {
        var builder1 = CreateDistributedApplicationBuilder();
        var elasticsearch1 = builder1.AddElasticsearch("elasticsearch");

        // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
        var volumeName = VolumeNameGenerator.CreateVolumeName(elasticsearch1, nameof(WithDataVolumeShouldPersistStateBetweenUsages));
        elasticsearch1.WithDataVolume(volumeName);

        var person = new Person
        {
            Id = 1,
            FirstName = "Alireza",
            LastName = "Baloochi"
        };

        var pipeline = new ResiliencePipelineBuilder()
              .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(3) })
              .AddTimeout(TimeSpan.FromSeconds(5))
              .Build();

        using (var app = builder1.Build())
        {
            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{elasticsearch1.Resource.Name}"] = await elasticsearch1.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
            });

            hb.AddElasticsearchClient(elasticsearch1.Resource.Name);

            using (var host = hb.Build())
            {
                await host.StartAsync();

                await pipeline.ExecuteAsync(
                    async token =>
                    {
                        var elasticsearchClient = host.Services.GetRequiredService<ElasticsearchClient>();

                        var indexResponse = await elasticsearchClient.IndexAsync<Person>(person, "people", "1", CancellationToken.None);

                        Assert.True(indexResponse.IsSuccess());
                    });
            }

            // Stops the container, or the Volume would still be in use
            await app.StopAsync();
        }

        var builder2 = CreateDistributedApplicationBuilder();
        var elasticsearch2 = builder2.AddElasticsearch("elasticsearch").WithDataVolume(volumeName);

        using (var app = builder2.Build())
        {
            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{elasticsearch2.Resource.Name}"] = await elasticsearch2.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
            });

            hb.AddElasticsearchClient(elasticsearch2.Resource.Name);

            using (var host = hb.Build())
            {
                await host.StartAsync();
                await pipeline.ExecuteAsync(
                    async token =>
                    {
                        var elasticsearchClient = host.Services.GetRequiredService<ElasticsearchClient>();

                        var getResponse = await elasticsearchClient.GetAsync<Person>("people", "1", CancellationToken.None);

                        Assert.True(getResponse.IsSuccess());
                        Assert.NotNull(getResponse.Source);
                        Assert.Equal(person.Id, getResponse.Source?.Id);
                    });

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
        var elasticsearch1 = builder1.AddElasticsearch("elasticsearch").WithDataBindMount(bindMountPath);

        var person = new Person
        {
            Id = 1,
            FirstName = "Alireza",
            LastName = "Baloochi"
        };

        var pipeline = new ResiliencePipelineBuilder()
              .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(3) })
              .AddTimeout(TimeSpan.FromSeconds(5))
              .Build();

        using (var app = builder1.Build())
        {
            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{elasticsearch1.Resource.Name}"] = await elasticsearch1.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
            });

            hb.AddElasticsearchClient(elasticsearch1.Resource.Name);

            using (var host = hb.Build())
            {
                await host.StartAsync();

                await pipeline.ExecuteAsync(
                    async token =>
                    {
                        var elasticsearchClient = host.Services.GetRequiredService<ElasticsearchClient>();

                        var indexResponse = await elasticsearchClient.IndexAsync<Person>(person, "people", "1", CancellationToken.None);

                        Assert.True(indexResponse.IsSuccess());
                    });
            }

            // Stops the container, or the Volume would still be in use
            await app.StopAsync();
        }

        var builder2 = CreateDistributedApplicationBuilder();
        var elasticsearch2 = builder2.AddElasticsearch("elasticsearch").WithDataBindMount(bindMountPath);

        using (var app = builder2.Build())
        {
            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{elasticsearch2.Resource.Name}"] = await elasticsearch2.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
            });

            hb.AddElasticsearchClient(elasticsearch2.Resource.Name);

            using (var host = hb.Build())
            {
                await host.StartAsync();
                await pipeline.ExecuteAsync(
                    async token =>
                    {
                        var elasticsearchClient = host.Services.GetRequiredService<ElasticsearchClient>();

                        var getResponse = await elasticsearchClient.GetAsync<Person>("people", "1", CancellationToken.None);

                        Assert.True(getResponse.IsSuccess());
                        Assert.NotNull(getResponse.Source);
                        Assert.Equal(person.Id, getResponse.Source?.Id);
                    });

            }

            // Stops the container, or the Volume would still be in use
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

    private static TestDistributedApplicationBuilder CreateDistributedApplicationBuilder() =>
    TestDistributedApplicationBuilder.CreateWithTestContainerRegistry();

    private sealed class Person
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
    }

}

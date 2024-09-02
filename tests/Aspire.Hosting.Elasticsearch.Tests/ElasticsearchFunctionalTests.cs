// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Elasticsearch.Tests;

public class ElasticsearchFunctionalTests(ITestOutputHelper testOutputHelper)
{
    private const string IndexName = "people";
    private static readonly Person s_person = new()
    {
        Id = 1,
        FirstName = "Alireza",
        LastName = "Baloochi"
    };

    [Fact]
    [RequiresDocker]
    public async Task VerifyElasticsearchResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var pipeline = new ResiliencePipelineBuilder()
           .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(10) })
           .Build();

        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var elasticsearch = builder.AddElasticsearch("elasticsearch");

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration[$"ConnectionStrings:{elasticsearch.Resource.Name}"] = await elasticsearch.Resource.ConnectionStringExpression.GetValueAsync(default);

        hb.AddElasticsearchClient(elasticsearch.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        await pipeline.ExecuteAsync(
            async token =>
            {

                var elasticsearchClient = host.Services.GetRequiredService<ElasticsearchClient>();

                await CreateTestData(elasticsearchClient, testOutputHelper, token);
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

            var elasticsearch1 = builder1.AddElasticsearch("elasticsearch");

            var password = elasticsearch1.Resource.PasswordParameter.Value;

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.CreateVolumeName(elasticsearch1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);
                elasticsearch1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Directory.CreateTempSubdirectory().FullName;
                elasticsearch1.WithDataBindMount(bindMountPath);
            }

            using (var app = builder1.Build())
            {
                await app.StartAsync();

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration[$"ConnectionStrings:{elasticsearch1.Resource.Name}"] = await elasticsearch1.Resource.ConnectionStringExpression.GetValueAsync(default);

                    hb.AddElasticsearchClient(elasticsearch1.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(
                            async token =>
                            {
                                var elasticsearchClient = host.Services.GetRequiredService<ElasticsearchClient>();
                                await CreateTestData(elasticsearchClient, testOutputHelper, token);
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
            var passwordParameter2 = builder2.AddParameter("pwd");
            builder2.Configuration["Parameters:pwd"] = password;
            var elasticsearch2 = builder2.AddElasticsearch("elasticsearch", passwordParameter2);

            if (useVolume)
            {
                elasticsearch2.WithDataVolume(volumeName);
            }
            else
            {
                elasticsearch2.WithDataBindMount(bindMountPath!);
            }

            using (var app = builder2.Build())
            {
                await app.StartAsync();

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration[$"ConnectionStrings:{elasticsearch2.Resource.Name}"] = await elasticsearch2.Resource.ConnectionStringExpression.GetValueAsync(default);

                    hb.AddElasticsearchClient(elasticsearch2.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();
                        await pipeline.ExecuteAsync(
                            async token =>
                            {
                                var elasticsearchClient = host.Services.GetRequiredService<ElasticsearchClient>();

                                var getResponse = await elasticsearchClient.GetAsync<Person>(IndexName, s_person.Id, token);

                                Assert.True(getResponse.IsSuccess());
                                Assert.NotNull(getResponse.Source);
                                Assert.Equal(s_person.Id, getResponse.Source?.Id);
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

    private static async Task CreateTestData(ElasticsearchClient elasticsearchClient, ITestOutputHelper testOutputHelper, CancellationToken cancellationToken)
    {
        var indexResponse = await elasticsearchClient.IndexAsync<Person>(s_person, IndexName, s_person.Id, cancellationToken);

        var getResponse = await elasticsearchClient.GetAsync<Person>(IndexName, s_person.Id, cancellationToken);

        testOutputHelper.WriteLine(indexResponse.DebugInformation);
        testOutputHelper.WriteLine(getResponse.DebugInformation);

        Assert.True(indexResponse.IsSuccess());
        Assert.True(getResponse.IsSuccess());
        Assert.NotNull(getResponse.Source);
        Assert.Equal(s_person.Id, getResponse.Source?.Id);
    }

    private sealed class Person
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
    }

}

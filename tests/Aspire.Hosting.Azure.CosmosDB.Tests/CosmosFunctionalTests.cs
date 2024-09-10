// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.CosmosDB.Tests;

public class CosmosFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker(Reason = "CosmosDB emulator is needed for this test")]
    public async Task VerifyCosmosResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new()
            {
                MaxRetryAttempts = 10,
                Delay = TimeSpan.FromSeconds(10),
                BackoffType = DelayBackoffType.Linear,
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
            })
            .Build();

        // @sebastienros: we won't use netaspireci.azurecr.io since the image is on mcr.microsoft.com, so Create is the way to go
        using var builder = TestDistributedApplicationBuilder.Create(options => { }, testOutputHelper);

        var databaseName = "db1";

        var cosmos = builder.AddAzureCosmosDB("cosmos");
        var db = cosmos.AddDatabase(databaseName)
                       .RunAsEmulator();

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(default)
        });

        hb.AddAzureCosmosClient(db.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        // This needs to be outside the pipeline because the CosmosClient is disposed,
        // there is an exception in the pipeline
        using var cosmosClient = host.Services.GetRequiredService<CosmosClient>();

        await pipeline.ExecuteAsync(async token =>
        {
            Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName, cancellationToken: token);
            Container container = await database.CreateContainerIfNotExistsAsync("container1", "/id", cancellationToken: token);
            var query = new QueryDefinition("SELECT VALUE 1");

            var results = await container.GetItemQueryIterator<int>(query).ReadNextAsync(token);

            Assert.True(results.Count == 1);
            Assert.True(results.First() == 1);
        }, cts.Token);
    }
}

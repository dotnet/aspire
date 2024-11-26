// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class AzureCosmosDBEmulatorFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyWaitForOnCosmosDBEmulatorBlocksDependentResources()
    {
        // Cosmos can be pretty slow to spin up, lets give it plenty of time.
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddAzureCosmosDB("resource")
                              .RunAsEmulator()
                              .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                                       .WaitFor(resource);

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        await rns.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await rns.WaitForResourceHealthyAsync(resource.Resource.Name, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;

        await app.StopAsync();
    }

    [Fact(Skip = "Using CosmosDB emulator in integration tests leads to flaky tests - https://github.com/dotnet/aspire/issues/5820")]
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
        var containerName = "container1";

        var cosmos = builder.AddAzureCosmosDB("cosmos");
        var db = cosmos.AddDatabase(databaseName)
                       .RunAsEmulator();

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();
        hb.Configuration[$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(default);
        hb.AddAzureCosmosClient(db.Resource.Name);
        hb.AddCosmosDbContext<EFCoreCosmosDbContext>(db.Resource.Name, databaseName);

        using var host = hb.Build();

        await host.StartAsync();

        // This needs to be outside the pipeline because when the CosmosClient is disposed,
        // there is an exception in the pipeline
        using var cosmosClient = host.Services.GetRequiredService<CosmosClient>();
        using var dbContext = host.Services.GetRequiredService<EFCoreCosmosDbContext>();

        await pipeline.ExecuteAsync(async token =>
        {
            Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName, cancellationToken: token);
            Container container = await database.CreateContainerIfNotExistsAsync(containerName, "/id", cancellationToken: token);
            var query = new QueryDefinition("SELECT VALUE 1");

            var results = await container.GetItemQueryIterator<int>(query).ReadNextAsync(token);

            Assert.True(results.Count == 1);
            Assert.True(results.First() == 1);

            await dbContext.Database.EnsureCreatedAsync(token);
            dbContext.AddRange([new Entry(), new Entry()]);
            var count = await dbContext.SaveChangesAsync(token);
            Assert.Equal(2, count);
        }, cts.Token);
    }

    [Fact(Skip = "Using CosmosDB emulator in integration tests leads to flaky tests - https://github.com/dotnet/aspire/issues/5820")]
    [RequiresDocker]
    public async Task WithDataVolumeShouldPersistStateBetweenUsages()
    {
        // Use a volume to do a snapshot save

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

        var databaseName = "db";
        var containerName = "container";

        using var builder1 = TestDistributedApplicationBuilder.Create(options => { }, testOutputHelper);
        var cosmos1 = builder1.AddAzureCosmosDB("cosmos");

        // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
        var volumeName = VolumeNameGenerator.Generate(cosmos1, nameof(WithDataVolumeShouldPersistStateBetweenUsages));

        var db1 = cosmos1.AddDatabase(databaseName)
                       .RunAsEmulator(emulator => emulator.WithDataVolume(volumeName));

        // if the volume already exists (because of a crashing previous run), delete it
        DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);

        var testObject = new { id = "1", data = "assertionValue" };

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

                hb.AddAzureCosmosClient(db1.Resource.Name);

                using (var host = hb.Build())
                {
                    await host.StartAsync();

                    // This needs to be outside the pipeline because when the CosmosClient is disposed,
                    // there is an exception in the pipeline
                    using var cosmosClient = host.Services.GetRequiredService<CosmosClient>();

                    await pipeline.ExecuteAsync(async token =>
                    {
                        Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName, cancellationToken: token);
                        Container container = await database.CreateContainerIfNotExistsAsync(containerName, "/id", cancellationToken: token);

                        await container.CreateItemAsync(testObject, cancellationToken: token);
                    }, cts.Token);

                }
            }
            finally
            {
                // Stops the container, or the Volume/mount would still be in use
                await app.StopAsync();
            }
        }

        using var builder2 = TestDistributedApplicationBuilder.Create(options => { }, testOutputHelper);

        var cosmos2 = builder2.AddAzureCosmosDB("cosmos");
        var db2 = cosmos2.AddDatabase(databaseName)
                       .RunAsEmulator(emulator => emulator.WithDataVolume(volumeName));

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

                hb.AddAzureCosmosClient(db2.Resource.Name);

                using (var host = hb.Build())
                {
                    await host.StartAsync();

                    // This needs to be outside the pipeline because when the CosmosClient is disposed,
                    // there is an exception in the pipeline
                    using var cosmosClient = host.Services.GetRequiredService<CosmosClient>();

                    await pipeline.ExecuteAsync(async token =>
                    {
                        var container = cosmosClient.GetContainer(databaseName, containerName);

                        QueryDefinition query = new("SELECT VALUE data FROM c WHERE c.id = '1'");

                        // run query and check the value
                        var results = await container.GetItemQueryIterator<string>(query).ReadNextAsync(token);

                        Assert.True(results.Count == 1);
                        Assert.True(results.First() == testObject.data);

                    }, cts.Token);
                }
            }
            finally
            {
                // Stops the container, or the Volume/mount would still be in use
                await app.StopAsync();
            }
        }

        DockerUtils.AttemptDeleteDockerVolume(volumeName);
    }
}

public class EFCoreCosmosDbContext(DbContextOptions<EFCoreCosmosDbContext> options) : DbContext(options)
{
    public DbSet<Entry> Entries { get; set; }
}

public record Entry
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

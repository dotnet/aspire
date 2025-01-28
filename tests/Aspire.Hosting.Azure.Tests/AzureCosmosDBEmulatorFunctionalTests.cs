// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
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
    [Theory]
    // [InlineData(true)] // "Using CosmosDB emulator in integration tests leads to flaky tests - https://github.com/dotnet/aspire/issues/5820"
    [InlineData(false)]
    [RequiresDocker]
    public async Task VerifyWaitForOnCosmosDBEmulatorBlocksDependentResources(bool usePreview)
    {
        // Cosmos can be pretty slow to spin up, lets give it plenty of time.
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddAzureCosmosDB("resource")
                              .RunAsEmulator(usePreview)
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

    [Theory(Skip = "Using CosmosDB emulator in integration tests leads to flaky tests - https://github.com/dotnet/aspire/issues/5820")]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker(Reason = "CosmosDB emulator is needed for this test")]
    public async Task VerifyCosmosResource(bool usePreview)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
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
        var db = cosmos.WithDatabase(databaseName)
                       .RunAsEmulator(usePreview);

        using var app = builder.Build();

        await app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        await rns.WaitForResourceHealthyAsync(db.Resource.Name, cts.Token);

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

            var testObject = new { id = "1", data = "assertionValue" };
            await container.CreateItemAsync(testObject, cancellationToken: token);

            // run query and check the value
            QueryDefinition query = new("SELECT VALUE c.data FROM c WHERE c.id = '1'");
            var results = await container.GetItemQueryIterator<string>(query).ReadNextAsync(token);

            Assert.True(results.Count == 1);
            Assert.True(results.First() == testObject.data);

            await dbContext.Database.EnsureCreatedAsync(token);
            dbContext.AddRange([new Entry(), new Entry()]);
            var count = await dbContext.SaveChangesAsync(token);
            Assert.Equal(2, count);
        }, cts.Token);
    }

    [Theory(Skip = "Using CosmosDB emulator in integration tests leads to flaky tests - https://github.com/dotnet/aspire/issues/5820")]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker]
    public async Task WithDataVolumeShouldPersistStateBetweenUsages(bool usePreview)
    {
        // Use a volume to do a snapshot save

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
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

        var db1 = cosmos1.WithDatabase(databaseName)
                       .RunAsEmulator(usePreview, volumeName);

        // if the volume already exists (because of a crashing previous run), delete it
        DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);

        var testObject = new { id = "1", data = "assertionValue" };

        using (var app = builder1.Build())
        {
            await app.StartAsync(cts.Token);

            var rns = app.Services.GetRequiredService<ResourceNotificationService>();
            await rns.WaitForResourceHealthyAsync(db1.Resource.Name, cts.Token);

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
        var db2 = cosmos2.WithDatabase(databaseName)
                       .RunAsEmulator(usePreview, volumeName);

        using (var app = builder2.Build())
        {
            await app.StartAsync(cts.Token);

            var rns = app.Services.GetRequiredService<ResourceNotificationService>();
            await rns.WaitForResourceHealthyAsync(db2.Resource.Name, cts.Token);

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

                        QueryDefinition query = new("SELECT VALUE c.data FROM c WHERE c.id = '1'");

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

    [Fact]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/7178")]
    public async Task AddAzureCosmosDB_RunAsEmulator_CreatesDatabase()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

        using var builder = TestDistributedApplicationBuilder.Create(options => { }, testOutputHelper);

        var databaseName = "db1";
        var containerName = "container1";
        var partitionKeyPath = "/id";

        var cosmos = builder.AddAzureCosmosDB("cosmos")
                            .WithDatabase(databaseName, db => db.Containers.Add(new(containerName, partitionKeyPath)))
                            .RunAsEmulator();

        using var app = builder.Build();

        await app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        await rns.WaitForResourceHealthyAsync(cosmos.Resource.Name, cts.Token);

        var hb = Host.CreateApplicationBuilder();
        hb.Configuration[$"ConnectionStrings:{cosmos.Resource.Name}"] = await cosmos.Resource.ConnectionStringExpression.GetValueAsync(default);
        hb.AddAzureCosmosClient(cosmos.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync(cts.Token);

        using var cosmosClient = host.Services.GetRequiredService<CosmosClient>();

        var database = cosmosClient.GetDatabase(databaseName);
        var result1 = await database.ReadAsync(cancellationToken: cts.Token);

        var container = database.GetContainer(containerName);
        var result2 = await container.ReadContainerAsync(cancellationToken: cts.Token);

        Assert.True(IsSuccess(result1.StatusCode));
        Assert.True(IsSuccess(result2.StatusCode));

        static bool IsSuccess(HttpStatusCode httpStatusCode)
        {
            return ((int)httpStatusCode >= 200) && ((int)httpStatusCode <= 299);
        }
    }
}

public class EFCoreCosmosDbContext(DbContextOptions<EFCoreCosmosDbContext> options) : DbContext(options)
{
    public DbSet<Entry> Entries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entry>()
            .HasPartitionKey(e => e.Id);
    }
}

public record Entry
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

internal static class CosmosExtensions
{
    public static IResourceBuilder<AzureCosmosDBResource> RunAsEmulator(this IResourceBuilder<AzureCosmosDBResource> builder, bool usePreview, string? volumeName = null)
    {
        void WithVolume(IResourceBuilder<AzureCosmosDBEmulatorResource> emulator)
        {
            if (volumeName is not null)
            {
                emulator.WithDataVolume(volumeName);
            }
        }

        return usePreview
#pragma warning disable ASPIRECOSMOS001 // RunAsPreviewEmulator is experimental
            ? builder.RunAsPreviewEmulator(WithVolume)
#pragma warning restore ASPIRECOSMOS001 // RunAsPreviewEmulator is experimental
            : builder.RunAsEmulator(WithVolume);
    }
}

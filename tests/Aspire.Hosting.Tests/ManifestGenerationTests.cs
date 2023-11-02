// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ManifestGenerationTests
{
    [Fact]
    public void EnsureAllRedisManifestTypesHaveVersion0Suffix()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddRedis("redisconnection");
        program.AppBuilder.AddRedisContainer("rediscontainer");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var connection = resources.GetProperty("redisconnection");
        Assert.Equal("redis.v0", connection.GetProperty("type").GetString());

        var container = resources.GetProperty("rediscontainer");
        Assert.Equal("redis.v0", container.GetProperty("type").GetString());
    }

    [Fact]
    public void EnsureAllPostgresManifestTypesHaveVersion0Suffix()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddPostgresConnection("postgresconnection");
        program.AppBuilder.AddPostgresContainer("postgresserver").AddDatabase("postgresdatabase");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var connection = resources.GetProperty("postgresconnection");
        Assert.Equal("postgres.connection.v0", connection.GetProperty("type").GetString());

        var server = resources.GetProperty("postgresserver");
        Assert.Equal("postgres.server.v0", server.GetProperty("type").GetString());

        var db = resources.GetProperty("postgresdatabase");
        Assert.Equal("postgres.database.v0", db.GetProperty("type").GetString());
    }

    [Fact]
    public void EnsureAllAzureStorageManifestTypesHaveVersion0Suffix()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        var parent = program.AppBuilder.AddAzureStorage("storage");
        parent.AddBlobs("blobs");
        parent.AddQueues("queues");
        parent.AddTables("tables");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var storage = resources.GetProperty("storage");
        Assert.Equal("azure.storage.v0", storage.GetProperty("type").GetString());

        var blobs = resources.GetProperty("blobs");
        Assert.Equal("azure.storage.blob.v0", blobs.GetProperty("type").GetString());

        var queues = resources.GetProperty("queues");
        Assert.Equal("azure.storage.queue.v0", queues.GetProperty("type").GetString());

        var tables = resources.GetProperty("tables");
        Assert.Equal("azure.storage.table.v0", tables.GetProperty("type").GetString());
    }

    [Fact]
    public void EnsureAllRabitMQManifestTypesHaveVersion0Suffix()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddRabbitMQConnection("rabbitconnection");
        program.AppBuilder.AddRabbitMQContainer("rabbitcontainer");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var connection = resources.GetProperty("rabbitconnection");
        Assert.Equal("rabbitmq.connection.v0", connection.GetProperty("type").GetString());

        var server = resources.GetProperty("rabbitcontainer");
        Assert.Equal("rabbitmq.server.v0", server.GetProperty("type").GetString());
    }

    [Fact]
    public void EnsureAllKeyVaultManifestTypesHaveVersion0Suffix()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddAzureKeyVault("keyvault");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var keyvault = resources.GetProperty("keyvault");
        Assert.Equal("azure.keyvault.v0", keyvault.GetProperty("type").GetString());
    }

    [Fact]
    public void EnsureAllServiceBusManifestTypesHaveVersion0Suffix()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddAzureServiceBus("servicebus");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var servicebus = resources.GetProperty("servicebus");
        Assert.Equal("azure.servicebus.v0", servicebus.GetProperty("type").GetString());
    }

    [Fact]
    public void EnsureAllAzureRedisManifestTypesHaveVersion0Suffix()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddAzureRedis("redis");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var redis = resources.GetProperty("redis");
        Assert.Equal("azure.redis.v0", redis.GetProperty("type").GetString());
    }

    private static TestProgram CreateTestProgramJsonDocumentManifestPublisher()
    {
        var manifestPath = Path.GetTempFileName();
        var program = TestProgram.Create<ManifestGenerationTests>(["--publisher", "manifest", "--output-path", manifestPath]);
        program.AppBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, JsonDocumentManifestPublisher>("manifest");
        return program;
    }

    private static TestProgram CreateTestProgram(string[]? args = null) => TestProgram.Create<ManifestGenerationTests>(args);
}

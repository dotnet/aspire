// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ManifestGenerationTests
{
    [Fact]
    public void EnsureAllRedisManifestTypesHaveVersion0Suffix()
    {
        var manifestPath = Path.GetTempFileName();
        var program = CreateTestProgram(["--publisher", "manifest", "--output-path", manifestPath]);
        program.AppBuilder.AddRedis("redisconnection");
        program.AppBuilder.AddRedisContainer("rediscontainer");

        program.Run();

        var json = File.ReadAllText(manifestPath);
        var document = JsonDocument.Parse(json);

        var resources = document.RootElement.GetProperty("resources");

        var connection = resources.GetProperty("redisconnection");
        Assert.Equal("redis.v0", connection.GetProperty("type").GetString());

        var container = resources.GetProperty("rediscontainer");
        Assert.Equal("redis.v0", container.GetProperty("type").GetString());

        File.Delete(manifestPath);
    }

    [Fact]
    public void EnsureAllPostgresManifestTypesHaveVersion0Suffix()
    {
        var manifestPath = Path.GetTempFileName();
        var program = CreateTestProgram(["--publisher", "manifest", "--output-path", manifestPath]);
        program.AppBuilder.AddPostgresConnection("postgresconnection");
        program.AppBuilder.AddPostgresContainer("postgresserver").AddDatabase("postgresdatabase");

        program.Run();

        var json = File.ReadAllText(manifestPath);
        var document = JsonDocument.Parse(json);

        var resources = document.RootElement.GetProperty("resources");

        var connection = resources.GetProperty("postgresconnection");
        Assert.Equal("postgres.connection.v0", connection.GetProperty("type").GetString());

        var server = resources.GetProperty("postgresserver");
        Assert.Equal("postgres.server.v0", server.GetProperty("type").GetString());

        var db = resources.GetProperty("postgresdatabase");
        Assert.Equal("postgres.database.v0", db.GetProperty("type").GetString());

        File.Delete(manifestPath);
    }

    [Fact]
    public void EnsureAllAzureStorageManifestTypesHaveVersion0Suffix()
    {
        var manifestPath = Path.GetTempFileName();
        var program = CreateTestProgram(["--publisher", "manifest", "--output-path", manifestPath]);
        var parent = program.AppBuilder.AddAzureStorage("storage");
        parent.AddBlobs("blobs");
        parent.AddQueues("queues");
        parent.AddTables("tables");

        program.Run();

        var json = File.ReadAllText(manifestPath);
        var document = JsonDocument.Parse(json);

        var resources = document.RootElement.GetProperty("resources");

        var storage = resources.GetProperty("storage");
        Assert.Equal("azure.storage.v0", storage.GetProperty("type").GetString());

        var blobs = resources.GetProperty("blobs");
        Assert.Equal("azure.storage.blob.v0", blobs.GetProperty("type").GetString());

        var queues = resources.GetProperty("queues");
        Assert.Equal("azure.storage.queue.v0", queues.GetProperty("type").GetString());

        var tables = resources.GetProperty("tables");
        Assert.Equal("azure.storage.table.v0", tables.GetProperty("type").GetString());

        File.Delete(manifestPath);
    }

    [Fact]
    public void EnsureAllRabitMQManifestTypesHaveVersion0Suffix()
    {
        var manifestPath = Path.GetTempFileName();
        var program = CreateTestProgram(["--publisher", "manifest", "--output-path", manifestPath]);
        program.AppBuilder.AddRabbitMQConnection("rabbitconnection");
        program.AppBuilder.AddRabbitMQContainer("rabbitcontainer");

        program.Run();

        var json = File.ReadAllText(manifestPath);
        var document = JsonDocument.Parse(json);

        var resources = document.RootElement.GetProperty("resources");

        var connection = resources.GetProperty("rabbitconnection");
        Assert.Equal("rabbitmq.connection.v0", connection.GetProperty("type").GetString());

        var server = resources.GetProperty("rabbitcontainer");
        Assert.Equal("rabbitmq.server.v0", server.GetProperty("type").GetString());

        File.Delete(manifestPath);
    }

    [Fact]
    public void EnsureAllKeyVaultManifestTypesHaveVersion0Suffix()
    {
        var manifestPath = Path.GetTempFileName();
        var program = CreateTestProgram(["--publisher", "manifest", "--output-path", manifestPath]);
        program.AppBuilder.AddAzureKeyVault("keyvault");

        program.Run();

        var json = File.ReadAllText(manifestPath);
        var document = JsonDocument.Parse(json);

        var resources = document.RootElement.GetProperty("resources");

        var keyvault = resources.GetProperty("keyvault");
        Assert.Equal("azure.keyvault.v0", keyvault.GetProperty("type").GetString());

        File.Delete(manifestPath);
    }

    [Fact]
    public void EnsureAllServiceBusManifestTypesHaveVersion0Suffix()
    {
        var manifestPath = Path.GetTempFileName();
        var program = CreateTestProgram(["--publisher", "manifest", "--output-path", manifestPath]);
        program.AppBuilder.AddAzureServiceBus("servicebus");

        program.Run();

        var json = File.ReadAllText(manifestPath);
        var document = JsonDocument.Parse(json);

        var resources = document.RootElement.GetProperty("resources");

        var servicebus = resources.GetProperty("servicebus");
        Assert.Equal("azure.servicebus.v0", servicebus.GetProperty("type").GetString());

        File.Delete(manifestPath);
    }

    [Fact]
    public void EnsureAllAzureRedisManifestTypesHaveVersion0Suffix()
    {
        var manifestPath = Path.GetTempFileName();
        var program = CreateTestProgram(["--publisher", "manifest", "--output-path", manifestPath]);
        program.AppBuilder.AddAzureRedis("redis");

        program.Run();

        var json = File.ReadAllText(manifestPath);
        var document = JsonDocument.Parse(json);

        var resources = document.RootElement.GetProperty("resources");

        var redis = resources.GetProperty("redis");
        Assert.Equal("azure.redis.v0", redis.GetProperty("type").GetString());

        File.Delete(manifestPath);
    }

    [Fact]
    public void EnsureAllAzureAppConfigurationManifestTypesHaveVersion0Suffix()
    {
        var manifestPath = Path.GetTempFileName();
        var program = CreateTestProgram(["--publisher", "manifest", "--output-path", manifestPath]);
        program.AppBuilder.AddAzureAppConfiguration("appconfig");

        program.Run();

        var json = File.ReadAllText(manifestPath);
        var document = JsonDocument.Parse(json);

        var resources = document.RootElement.GetProperty("resources");

        var config = resources.GetProperty("appconfig");
        Assert.Equal("azure.appconfiguration.v0", config.GetProperty("type").GetString());

        File.Delete(manifestPath);
    }

    private static TestProgram CreateTestProgram(string[]? args = null) => TestProgram.Create<ManifestGenerationTests>(args);
}

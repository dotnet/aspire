// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ManifestGenerationTests
{
    [Fact]
    public void EnsureWorkerProjectDoesNotGetBindingsGenerated()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var workerA = resources.GetProperty("workera");
        Assert.False(workerA.TryGetProperty("bindings", out _));
    }

    [Fact]
    public void EnsureExecutablesWithDockerfileProduceDockerfilev0Manifest()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher(includeNodeApp: true);
        program.NodeAppBuilder!.AsDockerfileInManifest();

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        // NPM app should still be executable.v0
        var npmapp = resources.GetProperty("npmapp");
        Assert.Equal("executable.v0", npmapp.GetProperty("type").GetString());

        // Node app should now be dockerfile.v0
        var nodeapp = resources.GetProperty("nodeapp");
        Assert.Equal("dockerfile.v0", nodeapp.GetProperty("type").GetString());
        Assert.True(nodeapp.TryGetProperty("path", out _));
        Assert.True(nodeapp.TryGetProperty("context", out _));
        Assert.True(nodeapp.TryGetProperty("env", out _));
        Assert.True(nodeapp.TryGetProperty("bindings", out _));
    }

    [Fact]
    public void EnsureContainerWithServiceBindingsEmitsContainerPort()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddContainer("grafana", "grafana/grafana")
                          .WithServiceBinding(3000, scheme: "http");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var grafana = resources.GetProperty("grafana");
        var bindings = grafana.GetProperty("bindings");
        var httpBinding = bindings.GetProperty("http");
        Assert.Equal(3000, httpBinding.GetProperty("containerPort").GetInt32());
    }

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

    [Fact]
    public void EnsureAllAzureAppConfigurationManifestTypesHaveVersion0Suffix()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddAzureAppConfiguration("appconfig");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var config = resources.GetProperty("appconfig");
        Assert.Equal("azure.appconfiguration.v0", config.GetProperty("type").GetString());
    }

    [Fact]
    public void NodeAppIsExecutableResource()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddNodeApp("nodeapp", "..\\foo\\app.js")
            .WithServiceBinding(hostPort: 5031, scheme: "http", env: "PORT");
        program.AppBuilder.AddNpmApp("npmapp", "..\\foo")
            .WithServiceBinding(hostPort: 5032, scheme: "http", env: "PORT");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var nodeApp = resources.GetProperty("nodeapp");
        var npmApp = resources.GetProperty("npmapp");

        static void AssertNodeResource(string resourceName, JsonElement jsonElement, string expectedCommand, string[] expectedArgs)
        {
            Assert.Equal("executable.v0", jsonElement.GetProperty("type").GetString());

            var bindings = jsonElement.GetProperty("bindings");
            var httpBinding = bindings.GetProperty("http");

            Assert.Equal("http", httpBinding.GetProperty("scheme").GetString());

            var env = jsonElement.GetProperty("env");
            Assert.Equal($$"""{{{resourceName}}.bindings.http.port}""", env.GetProperty("PORT").GetString());
            Assert.Equal("production", env.GetProperty("NODE_ENV").GetString());

            var command = jsonElement.GetProperty("command");
            Assert.Equal(expectedCommand, command.GetString());
            Assert.Equal(expectedArgs, jsonElement.GetProperty("args").EnumerateArray().Select(e => e.GetString()).ToArray());

            var args = jsonElement.GetProperty("args");
        }

        AssertNodeResource("nodeapp", nodeApp, "node", ["..\\foo\\app.js"]);
        AssertNodeResource("npmapp", npmApp, "npm", ["run", "start"]);
    }

    private static TestProgram CreateTestProgramJsonDocumentManifestPublisher(bool includeNodeApp = false)
    {
        var manifestPath = Path.GetTempFileName();
        var program = TestProgram.Create<ManifestGenerationTests>(["--publisher", "manifest", "--output-path", manifestPath], includeNodeApp: includeNodeApp);
        program.AppBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, JsonDocumentManifestPublisher>("manifest");
        return program;
    }

    private static TestProgram CreateTestProgram(string[]? args = null) => TestProgram.Create<ManifestGenerationTests>(args);
}

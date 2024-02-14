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
        program.NodeAppBuilder!.WithHttpsEndpoint(containerPort: 3000, env: "HTTPS_PORT")
            .PublishAsDockerFile();

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        // NPM app should still be executable.v0
        var npmapp = resources.GetProperty("npmapp");
        Assert.Equal("executable.v0", npmapp.GetProperty("type").GetString());
        Assert.DoesNotContain("\\", npmapp.GetProperty("workingDirectory").GetString());

        // Node app should now be dockerfile.v0
        var nodeapp = resources.GetProperty("nodeapp");
        Assert.Equal("dockerfile.v0", nodeapp.GetProperty("type").GetString());
        Assert.True(nodeapp.TryGetProperty("path", out _));
        Assert.True(nodeapp.TryGetProperty("context", out _));
        Assert.True(nodeapp.TryGetProperty("env", out var env));
        Assert.True(nodeapp.TryGetProperty("bindings", out var bindings));

        Assert.Equal(3000, bindings.GetProperty("https").GetProperty("containerPort").GetInt32());
        Assert.Equal("https", bindings.GetProperty("https").GetProperty("scheme").GetString());
        Assert.Equal("{nodeapp.bindings.https.port}", env.GetProperty("HTTPS_PORT").GetString());
    }

    [Fact]
    public void ExcludeLaunchProfileOmitsBindings()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();
        program.ServiceABuilder.ExcludeLaunchProfile();

        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        Assert.False(
            resources.GetProperty("servicea").TryGetProperty("bindings", out _),
            "Service has no bindings because they weren't populated from the launch profile.");
    }

    [Fact]
    public void EnsureContainerWithEndpointsEmitsContainerPort()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddContainer("grafana", "grafana/grafana")
                          .WithHttpEndpoint(3000);

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
    public void EnsureContainerWithArgsEmitsContainerArgs()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddContainer("grafana", "grafana/grafana")
                          .WithArgs("test", "arg2", "more");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var grafana = resources.GetProperty("grafana");
        var args = grafana.GetProperty("args");
        Assert.Equal(3, args.GetArrayLength());
        Assert.Collection(args.EnumerateArray(),
            arg => Assert.Equal("test", arg.GetString()),
            arg => Assert.Equal("arg2", arg.GetString()),
            arg => Assert.Equal("more", arg.GetString()));
    }

    [Theory]
    [InlineData(new string[] { "args1", "args2" }, new string[] { "withArgs1", "withArgs2" })]
    [InlineData(new string[] { }, new string[] { "withArgs1", "withArgs2" })]
    [InlineData(new string[] { "args1", "args2" }, new string[] { })]
    public void EnsureExecutableWithArgsEmitsExecutableArgs(string[] addExecutableArgs, string[] withArgsArgs)
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        var resourceBuilder = program.AppBuilder.AddExecutable("program", "run program", "c:/", addExecutableArgs);
        if (withArgsArgs.Length > 0)
        {
            resourceBuilder.WithArgs(withArgsArgs);
        }

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var resource = resources.GetProperty("program");
        var args = resource.GetProperty("args");
        Assert.Equal(addExecutableArgs.Length + withArgsArgs.Length, args.GetArrayLength());

        var verify = new List<Action<JsonElement>>();
        foreach (var addExecutableArg in addExecutableArgs)
        {
            verify.Add(arg => Assert.Equal(addExecutableArg, arg.GetString()));
        }
        foreach (var withArgsArg in withArgsArgs)
        {
            verify.Add(arg => Assert.Equal(withArgsArg, arg.GetString()));
        }

        Assert.Collection(args.EnumerateArray(), [.. verify]);
    }

    [Fact]
    public void ExecutableManifestNotIncludeArgsWhenEmpty()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddExecutable("program", "run program", "c:/");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var resource = resources.GetProperty("program");
        var exists = resource.TryGetProperty("args", out _);
        Assert.False(exists);
    }

    [Fact]
    public void EnsureContainerWithCustomEntrypointEmitsEntrypoint()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        var container = program.AppBuilder.AddContainer("grafana", "grafana/grafana");
        container.Resource.Entrypoint = "custom";

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var grafana = resources.GetProperty("grafana");
        var entrypoint = grafana.GetProperty("entrypoint");
        Assert.Equal("custom", entrypoint.GetString());
    }

    [Fact]
    public void EnsureAllRedisManifestTypesHaveVersion0Suffix()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddRedis("redisabstract");
        program.AppBuilder.AddRedis("rediscontainer").PublishAsContainer();

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var connection = resources.GetProperty("redisabstract");
        Assert.Equal("redis.v0", connection.GetProperty("type").GetString());

        var container = resources.GetProperty("rediscontainer");
        Assert.Equal("container.v0", container.GetProperty("type").GetString());
    }

    [Fact]
    public void PublishingRedisResourceAsContainerResultsInConnectionStringProperty()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddRedis("rediscontainer").PublishAsContainer();

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var container = resources.GetProperty("rediscontainer");
        Assert.Equal("container.v0", container.GetProperty("type").GetString());
        Assert.Equal("{rediscontainer.bindings.tcp.host}:{rediscontainer.bindings.tcp.port}", container.GetProperty("connectionString").GetString());

    }

    [Fact]
    public void EnsureAllPostgresManifestTypesHaveVersion0Suffix()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddPostgres("postgresabstract");
        program.AppBuilder.AddPostgres("postgrescontainer").PublishAsContainer().AddDatabase("postgresdatabase");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var connection = resources.GetProperty("postgresabstract");
        Assert.Equal("postgres.server.v0", connection.GetProperty("type").GetString());

        var server = resources.GetProperty("postgrescontainer");
        Assert.Equal("container.v0", server.GetProperty("type").GetString());

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
    public void EnsureAllRabbitMQManifestTypesHaveVersion0Suffix()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddRabbitMQ("rabbitabstract");
        program.AppBuilder.AddRabbitMQ("rabbitcontainer").PublishAsContainer();

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var connection = resources.GetProperty("rabbitabstract");
        Assert.Equal("rabbitmq.server.v0", connection.GetProperty("type").GetString());

        var server = resources.GetProperty("rabbitcontainer");
        Assert.Equal("container.v0", server.GetProperty("type").GetString());
    }

    [Fact]
    public void EnsureAllKafkaManifestTypesHaveVersion0Suffix()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddKafka("kafkaabstract");
        program.AppBuilder.AddKafka("kafkacontainer").PublishAsContainer();

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var connection = resources.GetProperty("kafkaabstract");
        Assert.Equal("kafka.server.v0", connection.GetProperty("type").GetString());

        var server = resources.GetProperty("kafkacontainer");
        Assert.Equal("container.v0", server.GetProperty("type").GetString());
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
    public void EnsureAllAzureDatabaseManifestTypesHaveVersion0Suffix()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddAzureSqlServer("sqlserver").AddDatabase("database");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var sqlserver = resources.GetProperty("sqlserver");
        Assert.Equal("azure.sql.v0", sqlserver.GetProperty("type").GetString());

        var database = resources.GetProperty("database");
        Assert.Equal("azure.sql.database.v0", database.GetProperty("type").GetString());
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
    public void EnsureAllAzureOpenAIManifestTypesHaveVersion0Suffix()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddAzureOpenAI("openai").AddDeployment("deployment");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var openai = resources.GetProperty("openai");
        Assert.Equal("azure.openai.account.v0", openai.GetProperty("type").GetString());

        var deployment = resources.GetProperty("deployment");
        Assert.Equal("azure.openai.deployment.v0", deployment.GetProperty("type").GetString());
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
    public void EnsureAllAzureCosmosDBManifestTypesHaveVersion0Suffix()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddAzureCosmosDB("cosmosconnection", "a connection string").AddDatabase("mydb1");
        program.AppBuilder.AddAzureCosmosDB("cosmosaccount").AddDatabase("mydb2");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var connection = resources.GetProperty("cosmosconnection");
        Assert.Equal("azure.cosmosdb.connection.v0", connection.GetProperty("type").GetString());

        var account = resources.GetProperty("cosmosaccount");
        Assert.Equal("azure.cosmosdb.account.v0", account.GetProperty("type").GetString());

        var db1 = resources.GetProperty("mydb1");
        Assert.Equal("azure.cosmosdb.database.v0", db1.GetProperty("type").GetString());
        Assert.Equal("cosmosconnection", db1.GetProperty("parent").GetString());

        var db2 = resources.GetProperty("mydb2");
        Assert.Equal("azure.cosmosdb.database.v0", db2.GetProperty("type").GetString());
        Assert.Equal("cosmosaccount", db2.GetProperty("parent").GetString());
    }

    [Fact]
    public void EnsureAllAzureApplicationInsightsManifestTypesHaveVersion0Suffix()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddApplicationInsights("appInsights");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var storage = resources.GetProperty("appInsights");
        Assert.Equal("azure.appinsights.v0", storage.GetProperty("type").GetString());
    }

    [Fact]
    public void NodeAppIsExecutableResource()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddNodeApp("nodeapp", "..\\foo\\app.js")
            .WithHttpEndpoint(hostPort: 5031, env: "PORT");
        program.AppBuilder.AddNpmApp("npmapp", "..\\foo")
            .WithHttpEndpoint(hostPort: 5032, env: "PORT");

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

    [Fact]
    public void MetadataPropertyNotEmittedWhenMetadataNotAdded()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddContainer("testresource", "testresource");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var container = resources.GetProperty("testresource");
        Assert.False(container.TryGetProperty("metadata", out var _));
    }

    [Fact]
    public void MetadataPropertyEmittedWhenMetadataNotAdded()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddContainer("testresource", "testresource")
                          .WithMetadata("data", "value");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var container = resources.GetProperty("testresource");
        Assert.True(container.TryGetProperty("metadata", out var metadata));
        Assert.True(metadata.TryGetProperty("data", out var data));
        Assert.Equal("value", data.GetString());
    }

    [Fact]
    public void MetadataPropertyCanEmitComplexObjects()
    {
        var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddContainer("testresource", "testresource")
                          .WithMetadata("data", new
                          {
                              complexValue1 = 1,
                              complexValue2 = "s",
                              complexValue3 = true,
                              complexValue4 = new
                              {
                                  nestedComplexValue = DateTime.MinValue
                              }
                          });

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var container = resources.GetProperty("testresource");
        Assert.True(container.TryGetProperty("metadata", out var metadata));
        Assert.True(metadata.TryGetProperty("data", out var data));
        Assert.True(data.TryGetProperty("complexValue1", out var complexValue1));
        Assert.Equal(1, complexValue1.GetInt32());
        Assert.True(data.TryGetProperty("complexValue2", out var complexValue2));
        Assert.Equal("s", complexValue2.GetString());
        Assert.True(data.TryGetProperty("complexValue3", out var complexValue3));
        Assert.True(complexValue3.GetBoolean());
        Assert.True(data.TryGetProperty("complexValue4", out var complexValue4));
        Assert.True(complexValue4.TryGetProperty("nestedComplexValue", out var nestedComplexValue));
        Assert.Equal(DateTime.MinValue, nestedComplexValue.GetDateTime());
    }

    private static TestProgram CreateTestProgramJsonDocumentManifestPublisher(bool includeNodeApp = false)
    {
        var manifestPath = Path.GetTempFileName();
        var program = TestProgram.Create<ManifestGenerationTests>(["--publisher", "manifest", "--output-path", manifestPath], includeNodeApp: includeNodeApp);
        program.AppBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, JsonDocumentManifestPublisher>("manifest");
        return program;
    }
}

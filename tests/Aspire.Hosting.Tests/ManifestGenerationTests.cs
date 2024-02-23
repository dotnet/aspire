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
    public void EnsureAddParameterWithSecretFalseDoesntEmitSecretField()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();
        program.AppBuilder.AddParameter("x", secret: false);
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");
        var x = resources.GetProperty("x");
        var inputs = x.GetProperty("inputs");
        var value = inputs.GetProperty("value");
        Assert.False(value.TryGetProperty("secret", out _));
    }

    [Fact]
    public void EnsureAddParameterWithSecretDefaultDoesntEmitSecretField()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();
        program.AppBuilder.AddParameter("x");
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");
        var x = resources.GetProperty("x");
        var inputs = x.GetProperty("inputs");
        var value = inputs.GetProperty("value");
        Assert.False(value.TryGetProperty("secret", out _));
    }

    [Fact]
    public void EnsureAddParameterWithSecretTrueDoesEmitSecretField()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();
        program.AppBuilder.AddParameter("x", secret: true);
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");
        var x = resources.GetProperty("x");
        var inputs = x.GetProperty("inputs");
        var value = inputs.GetProperty("value");
        Assert.True(value.TryGetProperty("secret", out var secret));
        Assert.True(secret.GetBoolean());
    }

    [Fact]
    public void EnsureWorkerProjectDoesNotGetBindingsGenerated()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

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
        using var program = CreateTestProgramJsonDocumentManifestPublisher(includeNodeApp: true);
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
        using var program = CreateTestProgramJsonDocumentManifestPublisher();
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
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

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
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

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
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

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
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

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
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

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
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddRedis("rediscontainer");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var container = resources.GetProperty("rediscontainer");
        Assert.Equal("container.v0", container.GetProperty("type").GetString());
    }

    [Fact]
    public void PublishingRedisResourceAsContainerResultsInConnectionStringProperty()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddRedis("rediscontainer");

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
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddPostgres("postgrescontainer").AddDatabase("postgresdatabase");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var server = resources.GetProperty("postgrescontainer");
        Assert.Equal("container.v0", server.GetProperty("type").GetString());

        var db = resources.GetProperty("postgresdatabase");
        Assert.Equal("value.v0", db.GetProperty("type").GetString());
    }

    [Fact]
    public void EnsureAllRabbitMQManifestTypesHaveVersion0Suffix()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddRabbitMQ("rabbitcontainer");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var server = resources.GetProperty("rabbitcontainer");
        Assert.Equal("container.v0", server.GetProperty("type").GetString());
    }

    [Fact]
    public void EnsureAllKafkaManifestTypesHaveVersion0Suffix()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddKafka("kafkacontainer");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var server = resources.GetProperty("kafkacontainer");
        Assert.Equal("container.v0", server.GetProperty("type").GetString());
    }

    [Fact]
    public void EnsureAllAzureOpenAIManifestTypesHaveVersion0Suffix()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

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
    public void NodeAppIsExecutableResource()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

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
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

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
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

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
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

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

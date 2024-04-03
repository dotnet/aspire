// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithEnvironmentTests
{
    [Fact]
    public async Task EnvironmentReferencingEndpointPopulatesWithBindingUrl()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("project")
                              .WithHttpsEndpoint(port: 1000, targetPort: 2000, "mybinding")
                              .WithEndpoint("mybinding", e =>
                              {
                                  e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000);
                              });

        var projectB = builder.AddProject<ProjectB>("projectB")
                               .WithEnvironment("myName", projectA.GetEndpoint("mybinding"));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectB.Resource);

        Assert.Equal("https://localhost:2000", config["myName"]);
    }

    [Fact]
    public async Task SimpleEnvironmentWithNameAndValue()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var project = builder.AddProject<ProjectA>("projectA")
            .WithEnvironment("myName", "value");

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(project.Resource);

        Assert.Equal("value", config["myName"]);
    }

    [Fact]
    public async Task EnvironmentCallbackPopulatesValueWhenCalled()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var environmentValue = "value";
        var projectA = builder.AddProject<ProjectA>("projectA").WithEnvironment("myName", () => environmentValue);

        environmentValue = "value2";

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectA.Resource);

        Assert.Equal("value2", config["myName"]);
    }

    [Fact]
    public async Task EnvironmentCallbackPopulatesValueWhenParameterResourceProvided()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.Configuration["Parameters:parameter"] = "MY_PARAMETER_VALUE";

        var parameter = builder.AddParameter("parameter");

        var projectA = builder.AddProject<ProjectA>("projectA")
            .WithEnvironment("MY_PARAMETER", parameter);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectA.Resource);

        Assert.Equal("MY_PARAMETER_VALUE", config["MY_PARAMETER"]);
    }

    [Fact]
    public async Task EnvironmentCallbackPopulatesWithExpressionPlaceholderWhenPublishingManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var parameter = builder.AddParameter("parameter");

        var projectA = builder.AddProject<ProjectA>("projectA")
            .WithEnvironment("MY_PARAMETER", parameter);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectA.Resource,
            DistributedApplicationOperation.Publish);

        Assert.Equal("{parameter.value}", config["MY_PARAMETER"]);
    }

    [Fact]
    public async Task EnvironmentCallbackThrowsWhenParameterValueMissingInDcpMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var parameter = builder.AddParameter("parameter");

        var projectA = builder.AddProject<ProjectA>("projectA")
            .WithEnvironment("MY_PARAMETER", parameter);

        var exception = await Assert.ThrowsAsync<DistributedApplicationException>(async () => await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectA.Resource));

        Assert.Equal("Parameter resource could not be used because configuration key 'Parameters:parameter' is missing and the Parameter has no default value.", exception.Message);
    }

    [Fact]
    public async Task ComplexEnvironmentCallbackPopulatesValueWhenCalled()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var environmentValue = "value";
        var projectA = builder.AddProject<ProjectA>("projectA")
                              .WithEnvironment(context =>
                              {
                                  context.EnvironmentVariables["myName"] = environmentValue;
                              });

        environmentValue = "value2";

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectA.Resource);

        Assert.Equal("value2", config["myName"]);
    }

    [Fact]
    public async Task EnvironmentVariableExpressions()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var test = builder.AddResource(new TestResource("test", "connectionString"));

        var container = builder.AddContainer("container1", "image")
                               .WithHttpEndpoint(name: "primary", targetPort: 10005)
                               .WithEndpoint("primary", ep =>
                               {
                                   ep.AllocatedEndpoint = new AllocatedEndpoint(ep, "localhost", 90);
                               });

        var endpoint = container.GetEndpoint("primary");

        var containerB = builder.AddContainer("container2", "imageB")
                                .WithEnvironment("URL", $"{endpoint}/foo")
                                .WithEnvironment("PORT", $"{endpoint.Property(EndpointProperty.Port)}")
                                .WithEnvironment("TARGET_PORT", $"{endpoint.Property(EndpointProperty.TargetPort)}")
                                .WithEnvironment("HOST", $"{test.Resource};name=1");

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerB.Resource);
        var manifestConfig = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerB.Resource, DistributedApplicationOperation.Publish);

        Assert.Equal(4, config.Count);
        Assert.Equal($"http://localhost:90/foo", config["URL"]);
        Assert.Equal("90", config["PORT"]);
        Assert.Equal("10005", config["TARGET_PORT"]);
        Assert.Equal("connectionString;name=1", config["HOST"]);

        Assert.Equal(4, manifestConfig.Count);
        Assert.Equal("{container1.bindings.primary.url}/foo", manifestConfig["URL"]);
        Assert.Equal("{container1.bindings.primary.port}", manifestConfig["PORT"]);
        Assert.Equal("{container1.bindings.primary.targetPort}", manifestConfig["TARGET_PORT"]);
        Assert.Equal("{test.connectionString};name=1", manifestConfig["HOST"]);
    }

    [Fact]
    public async Task EnvironmentVariableWithDynamicTargetPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("container1", "image")
                               .WithHttpEndpoint(name: "primary")
                               .WithEndpoint("primary", ep =>
                               {
                                   ep.AllocatedEndpoint = new AllocatedEndpoint(ep, "localhost", 90, targetPortExpression: """{{- portForServing "container1_primary" -}}""");
                               });

        var endpoint = container.GetEndpoint("primary");

        var containerB = builder.AddContainer("container2", "imageB")
                                .WithEnvironment("TARGET_PORT", $"{endpoint.Property(EndpointProperty.TargetPort)}");

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerB.Resource);

        var pair = Assert.Single(config);
        Assert.Equal("TARGET_PORT", pair.Key);
        Assert.Equal("""{{- portForServing "container1_primary" -}}""", pair.Value);
    }

    [Fact]
    public async Task EnvironmentWithConnectionStringSetsProperEnvironmentVariable()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        const string sourceCon = "sourceConnectionString";

        var sourceBuilder = builder.AddResource(new TestResource("sourceService", sourceCon));
        var targetBuilder = builder.AddContainer("targetContainer", "targetImage");

        string envVarName = "CUSTOM_CONNECTION_STRING";

        // Act
        targetBuilder.WithEnvironment(envVarName, sourceBuilder);

        // Call environment variable callbacks for the Run operation.
        var runConfig = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(targetBuilder.Resource, DistributedApplicationOperation.Run);

        // Assert
        Assert.Single(runConfig, kvp => kvp.Key == envVarName && kvp.Value == sourceCon);

        // Call environment variable callbacks for the Publish operation.
        var publishConfig = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(targetBuilder.Resource, DistributedApplicationOperation.Publish);

        // Assert
        Assert.Single(publishConfig, kvp => kvp.Key == envVarName && kvp.Value == "{sourceService.connectionString}");
    }

    private sealed class TestResource(string name, string connectionString) : Resource(name), IResourceWithConnectionString
    {
        public ReferenceExpression ConnectionStringExpression =>
            ReferenceExpression.Create($"{connectionString}");
    }

    private sealed class ProjectA : IProjectMetadata
    {
        public string ProjectPath => "projectA";

        public LaunchSettings LaunchSettings { get; } = new();
    }

    private sealed class ProjectB : IProjectMetadata
    {
        public string ProjectPath => "projectB";
        public LaunchSettings LaunchSettings { get; } = new();
    }
}

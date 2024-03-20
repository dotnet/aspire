// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithEnvironmentTests
{
    [Fact]
    public async Task EnvironmentReferencingEndpointPopulatesWithBindingUrl()
    {
        using var testProgram = CreateTestProgram();

        // Create a binding and its metching annotation (simulating DCP behavior)
        testProgram.ServiceABuilder.WithHttpsEndpoint(1000, 2000, "mybinding");
        testProgram.ServiceABuilder.WithEndpoint(
            "mybinding",
            e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        testProgram.ServiceBBuilder.WithEnvironment("myName", testProgram.ServiceABuilder.GetEndpoint("mybinding"));

        testProgram.Build();

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceBBuilder.Resource);

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("myName"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "myName" && kvp.Value == "https://localhost:2000");
    }

    [Fact]
    public async Task SimpleEnvironmentWithNameAndValue()
    {
        using var testProgram = CreateTestProgram();

        testProgram.ServiceABuilder.WithEnvironment("myName", "value");

        testProgram.Build();

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceABuilder.Resource);

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("myName"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "myName" && kvp.Value == "value");
    }

    [Fact]
    public async Task EnvironmentCallbackPopulatesValueWhenCalled()
    {
        using var testProgram = CreateTestProgram();

        var environmentValue = "value";
        testProgram.ServiceABuilder.WithEnvironment("myName", () => environmentValue);

        testProgram.Build();
        environmentValue = "value2";

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceABuilder.Resource);

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("myName"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "myName" && kvp.Value == "value2");
    }

    [Fact]
    public async Task EnvironmentCallbackPopulatesValueWhenParameterResourceProvided()
    {
        using var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Configuration["Parameters:parameter"] = "MY_PARAMETER_VALUE";
        var parameter = testProgram.AppBuilder.AddParameter("parameter");

        testProgram.ServiceABuilder.WithEnvironment("MY_PARAMETER", parameter);

        testProgram.Build();

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceABuilder.Resource);

        Assert.Contains(config, kvp => kvp.Key == "MY_PARAMETER" && kvp.Value == "MY_PARAMETER_VALUE");
    }

    [Fact]
    public async Task EnvironmentCallbackPopulatesWithExpressionPlaceholderWhenPublishingManifest()
    {
        using var testProgram = CreateTestProgram();
        var parameter = testProgram.AppBuilder.AddParameter("parameter");

        testProgram.ServiceABuilder.WithEnvironment("MY_PARAMETER", parameter);

        testProgram.Build();

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceABuilder.Resource,
            DistributedApplicationOperation.Publish);

        Assert.Contains(config, kvp => kvp.Key == "MY_PARAMETER" && kvp.Value == "{parameter.value}");
    }

    [Fact]
    public async Task EnvironmentCallbackThrowsWhenParameterValueMissingInDcpMode()
    {
        using var testProgram = CreateTestProgram();
        var parameter = testProgram.AppBuilder.AddParameter("parameter");

        testProgram.ServiceABuilder.WithEnvironment("MY_PARAMETER", parameter);

        testProgram.Build();

        var annotations = testProgram.ServiceABuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var exception = await Assert.ThrowsAsync<DistributedApplicationException>(async () => await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceABuilder.Resource));

        Assert.Equal("Parameter resource could not be used because configuration key 'Parameters:parameter' is missing.", exception.Message);
    }

    [Fact]
    public async Task ComplexEnvironmentCallbackPopulatesValueWhenCalled()
    {
        using var testProgram = CreateTestProgram();

        var environmentValue = "value";
        testProgram.ServiceABuilder.WithEnvironment((context) =>
        {
            context.EnvironmentVariables["myName"] = environmentValue;
        });

        testProgram.Build();
        environmentValue = "value2";

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceABuilder.Resource);

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("myName"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "myName" && kvp.Value == "value2");
    }

    [Fact]
    public async Task EnvironmentVariableExpressions()
    {
        var builder = DistributedApplication.CreateBuilder();

        var test = builder.AddResource(new TestResource("test", "connectionString"));

        var container = builder.AddContainer("container1", "image")
                               .WithHttpEndpoint(name: "primary")
                               .WithEndpoint("primary", ep =>
                               {
                                   ep.AllocatedEndpoint = new AllocatedEndpoint(ep, "localhost", 90);
                               });

        var endpoint = container.GetEndpoint("primary");

        var containerB = builder.AddContainer("container2", "imageB")
                                .WithEnvironment("URL", $"{endpoint}/foo")
                                .WithEnvironment("PORT", $"{endpoint.Property(EndpointProperty.Port)}")
                                .WithEnvironment("HOST", $"{test.Resource};name=1");

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerB.Resource);
        var manifestConfig = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerB.Resource, DistributedApplicationOperation.Publish);

        Assert.Equal(3, config.Count);
        Assert.Equal($"http://localhost:90/foo", config["URL"]);
        Assert.Equal("90", config["PORT"]);
        Assert.Equal("connectionString;name=1", config["HOST"]);

        Assert.Equal(3, manifestConfig.Count);
        Assert.Equal("{container1.bindings.primary.url}/foo", manifestConfig["URL"]);
        Assert.Equal("{container1.bindings.primary.port}", manifestConfig["PORT"]);
        Assert.Equal("{test.connectionString};name=1", manifestConfig["HOST"]);
    }

    private sealed class TestResource(string name, string connectionString) : Resource(name), IResourceWithConnectionString
    {
        public ReferenceExpression ConnectionStringExpression =>
            ReferenceExpression.Create($"{connectionString}");
    }

    private static TestProgram CreateTestProgram(string[]? args = null) => TestProgram.Create<WithReferenceTests>(args);
}

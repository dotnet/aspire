// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
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
        testProgram.ServiceABuilder.WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding",
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ));

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

        Assert.Equal("Parameter resource could not be used because configuration key `Parameters:parameter` is missing.", exception.Message);
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
    private static TestProgram CreateTestProgram(string[]? args = null) => TestProgram.Create<WithReferenceTests>(args);
}

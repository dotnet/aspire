// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithEnvironmentTests
{
    [Fact]
    public void EnvironmentReferencingEndpointPopulatesWithBindingUrl()
    {
        var testProgram = CreateTestProgram();

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

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceBBuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var context = new EnvironmentCallbackContext(executionContext, config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("myName"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "myName" && kvp.Value == "https://localhost:2000");
    }

    [Fact]
    public void SimpleEnvironmentWithNameAndValue()
    {
        var testProgram = CreateTestProgram();

        testProgram.ServiceABuilder.WithEnvironment("myName", "value");

        testProgram.Build();

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceABuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var context = new EnvironmentCallbackContext(executionContext, config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("myName"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "myName" && kvp.Value == "value");
    }

    [Fact]
    public void EnvironmentCallbackPopulatesValueWhenCalled()
    {
        var testProgram = CreateTestProgram();

        var environmentValue = "value";
        testProgram.ServiceABuilder.WithEnvironment("myName", () => environmentValue);

        testProgram.Build();
        environmentValue = "value2";

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceABuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var context = new EnvironmentCallbackContext(executionContext, config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("myName"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "myName" && kvp.Value == "value2");
    }

    [Fact]
    public void EnvironmentCallbackPopulatesValueWhenParameterResourceProvided()
    {
        var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Configuration["Parameters:parameter"] = "MY_PARAMETER_VALUE";
        var parameter = testProgram.AppBuilder.AddParameter("parameter");

        testProgram.ServiceABuilder.WithEnvironment("MY_PARAMETER", parameter);

        testProgram.Build();

        var annotations = testProgram.ServiceABuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var context = new EnvironmentCallbackContext(executionContext, config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        Assert.Contains(config, kvp => kvp.Key == "MY_PARAMETER" && kvp.Value == "MY_PARAMETER_VALUE");
    }

    [Fact]
    public void EnvironmentCallbackPopulatesWithExpressionPlaceholderWhenPublishingManifest()
    {
        var testProgram = CreateTestProgram();
        var parameter = testProgram.AppBuilder.AddParameter("parameter");

        testProgram.ServiceABuilder.WithEnvironment("MY_PARAMETER", parameter);

        testProgram.Build();

        var annotations = testProgram.ServiceABuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
        var context = new EnvironmentCallbackContext(executionContext, config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        Assert.Contains(config, kvp => kvp.Key == "MY_PARAMETER" && kvp.Value == "{parameter.value}");
    }

    [Fact]
    public void EnvironmentCallbackThrowsWhenParameterValueMissingInDcpMode()
    {
        var testProgram = CreateTestProgram();
        var parameter = testProgram.AppBuilder.AddParameter("parameter");

        testProgram.ServiceABuilder.WithEnvironment("MY_PARAMETER", parameter);

        testProgram.Build();

        var annotations = testProgram.ServiceABuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var context = new EnvironmentCallbackContext(executionContext, config);

        var exception = Assert.Throws<DistributedApplicationException>(() =>
        {
            foreach (var annotation in annotations)
            {
                annotation.Callback(context);
            }
        });

        Assert.Equal("Parameter resource could not be used because configuration key `Parameters:parameter` is missing.", exception.Message);
    }

    [Fact]
    public void ComplexEnvironmentCallbackPopulatesValueWhenCalled()
    {
        var testProgram = CreateTestProgram();

        var environmentValue = "value";
        testProgram.ServiceABuilder.WithEnvironment((context) =>
        {
            context.EnvironmentVariables["myName"] = environmentValue;
        });

        testProgram.Build();
        environmentValue = "value2";

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceABuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var context = new EnvironmentCallbackContext(executionContext, config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("myName"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "myName" && kvp.Value == "value2");
    }
    private static TestProgram CreateTestProgram(string[]? args = null) => TestProgram.Create<WithReferenceTests>(args);
}

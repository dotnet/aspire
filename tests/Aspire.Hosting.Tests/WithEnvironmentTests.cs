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
        testProgram.ServiceABuilder.WithEndpoint(1000, 2000, "https", "mybinding");
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
        var context = new EnvironmentCallbackContext("dcp", config);

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
        var context = new EnvironmentCallbackContext("dcp", config);

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
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("myName"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "myName" && kvp.Value == "value2");
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
        var context = new EnvironmentCallbackContext("dcp", config);

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

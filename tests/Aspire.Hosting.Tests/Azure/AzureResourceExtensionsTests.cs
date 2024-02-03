// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests.Azure;

public class AzureResourceExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(8081)]
    [InlineData(9007)]
    public void AddAzureCosmosDBWithEmulatorGetsExpectedPort(int? port = null)
    {
        var builder = DistributedApplication.CreateBuilder();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.UseEmulator(port);

        var endpointAnnotation = cosmos.Resource.Annotations.OfType<EndpointAnnotation>().FirstOrDefault();
        Assert.NotNull(endpointAnnotation);

        var actualPort = endpointAnnotation.Port;
        Assert.Equal(port, actualPort);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2.3.97-preview")]
    [InlineData("1.0.7")]
    public void AddAzureCosmosDBWithEmulatorGetsExpectedImageTag(string? imageTag = null)
    {
        var builder = DistributedApplication.CreateBuilder();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.UseEmulator(imageTag: imageTag);

        var containerImageAnnotation = cosmos.Resource.Annotations.OfType<ContainerImageAnnotation>().FirstOrDefault();
        Assert.NotNull(containerImageAnnotation);

        var actualTag = containerImageAnnotation.Tag;
        Assert.Equal(imageTag ?? "latest", actualTag);
    }

    [Fact]
    public void WithReferenceAppInsightsWritesEnvVariableToManifest()
    {
        var builder = DistributedApplication.CreateBuilder();

        var ai = builder.AddApplicationInsights("ai");

        var serviceA = builder.AddProject<Projects.ServiceA>("serviceA")
            .WithReference(ai);

        // Call environment variable callbacks.
        var annotations = serviceA.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("manifest", config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeys = config.Keys.Where(key => key == "APPLICATIONINSIGHTS_CONNECTION_STRING");
        Assert.Single(servicesKeys);
        Assert.Contains(config, kvp => kvp.Key == "APPLICATIONINSIGHTS_CONNECTION_STRING" && kvp.Value == "{ai.connectionString}");
    }

    [Fact]
    public void WithReferenceAppInsightsSetsEnvironmentVariable()
    {
        var builder = DistributedApplication.CreateBuilder();

        var ai = builder.AddApplicationInsights("ai", "connectionString1234");

        var serviceA = builder.AddProject<Projects.ServiceA>("serviceA")
            .WithReference(ai);

        // Call environment variable callbacks.
        var annotations = serviceA.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeys = config.Keys.Where(key => key == "APPLICATIONINSIGHTS_CONNECTION_STRING");
        Assert.Single(servicesKeys);
        Assert.Contains(config, kvp => kvp.Key == "APPLICATIONINSIGHTS_CONNECTION_STRING" && kvp.Value == "connectionString1234");
    }
}

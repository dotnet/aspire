// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests.Azure;

public class AzureResourceExtensionsTests
{
    [Fact]
    public void AzureStorageUserEmulatorCallbackWithUsePersistenceResultsInVolumeAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var storage = builder.AddAzureStorage("storage").UseEmulator(configureContainer: builder =>
        {
            builder.UsePersistence("mydata");
        });

        var computedPath = Path.GetFullPath("mydata");

        var volumeAnnotation = storage.Resource.Annotations.OfType<VolumeMountAnnotation>().Single();
        Assert.Equal(computedPath, volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(VolumeMountType.Bind, volumeAnnotation.Type);
    }

    [Fact]
    public void AzureStorageUserEmulatorUseBlobQueueTablePortMethodsMutateEndpoints()
    {
        var builder = DistributedApplication.CreateBuilder();
        var storage = builder.AddAzureStorage("storage").UseEmulator(configureContainer: builder =>
        {
            builder.UseBlobPort(9001);
            builder.UseQueuePort(9002);
            builder.UseTablePort(9003);
        });

        Assert.Collection(
            storage.Resource.Annotations.OfType<EndpointAnnotation>(),
            e => Assert.Equal(9001, e.Port),
            e => Assert.Equal(9002, e.Port),
            e => Assert.Equal(9003, e.Port));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(8081)]
    [InlineData(9007)]
    public void AddAzureCosmosDBWithEmulatorGetsExpectedPort(int? port = null)
    {
        var builder = DistributedApplication.CreateBuilder();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.UseEmulator(container =>
        {
            container.UseGatewayPort(port);
        });

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

        cosmos.UseEmulator(container =>
        {
            container.WithImageTag(imageTag);
        });

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
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
        var context = new EnvironmentCallbackContext(executionContext, config);

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
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var context = new EnvironmentCallbackContext(executionContext, config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeys = config.Keys.Where(key => key == "APPLICATIONINSIGHTS_CONNECTION_STRING");
        Assert.Single(servicesKeys);
        Assert.Contains(config, kvp => kvp.Key == "APPLICATIONINSIGHTS_CONNECTION_STRING" && kvp.Value == "connectionString1234");
    }
}

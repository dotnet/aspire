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

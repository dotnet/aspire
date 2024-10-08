// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Aspire.Hosting.Azure.EventHubs;
using Xunit;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Aspire.Components.Common.Tests;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class AzureEventHubsExtensionsTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyWaitForOnEventHubsEmulatorBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddAzureEventHubs("resource")
                              .AddEventHub("hubx")
                              .RunAsEmulator()
                              .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                                       .WaitFor(resource);

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        await rns.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await rns.WaitForResourceHealthyAsync(resource.Resource.Name, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;

        await app.StopAsync();
    }

    [Fact]
    public void AzureEventHubsUseEmulatorCallbackWithWithDataBindMountResultsInBindMountAnnotationWithDefaultPath()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubs = builder.AddAzureEventHubs("eh").RunAsEmulator(configureContainer: builder =>
        {
            builder.WithDataBindMount();
        });

        // Ignoring the annotation created for the custom Config.json file
        var volumeAnnotation = eventHubs.Resource.Annotations.OfType<ContainerMountAnnotation>().Single(a => !a.Target.Contains("Config.json"));
        Assert.Equal(Path.Combine(builder.AppHostDirectory, ".eventhubs", "eh"), volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.False(volumeAnnotation.IsReadOnly);
    }

    [Fact]
    public void AzureEventHubsUseEmulatorCallbackWithWithDataBindMountResultsInBindMountAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubs = builder.AddAzureEventHubs("eh").RunAsEmulator(configureContainer: builder =>
        {
               builder.WithDataBindMount("mydata");
        });

        // Ignoring the annotation created for the custom Config.json file
        var volumeAnnotation = eventHubs.Resource.Annotations.OfType<ContainerMountAnnotation>().Single(a => !a.Target.Contains("Config.json"));
        Assert.Equal(Path.Combine(builder.AppHostDirectory, "mydata"), volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.False(volumeAnnotation.IsReadOnly);
    }

    [Fact]
    public void AzureEventHubsUseEmulatorCallbackWithWithDataVolumeResultsInVolumeAnnotationWithDefaultName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubs = builder.AddAzureEventHubs("eh").RunAsEmulator(configureContainer: builder =>
        {
            builder.WithDataVolume();
        });

        // Ignoring the annotation created for the custom Config.json file
        var volumeAnnotation = eventHubs.Resource.Annotations.OfType<ContainerMountAnnotation>().Single(a => !a.Target.Contains("Config.json"));
        Assert.Equal($"{builder.GetVolumePrefix()}-eh-data", volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.False(volumeAnnotation.IsReadOnly);
    }

    [Fact]
    public void AzureEventHubsUseEmulatorCallbackWithWithDataVolumeResultsInVolumeAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubs = builder.AddAzureEventHubs("eh").RunAsEmulator(configureContainer: builder =>
        {
            builder.WithDataVolume("mydata");
        });

        // Ignoring the annotation created for the custom Config.json file
        var volumeAnnotation = eventHubs.Resource.Annotations.OfType<ContainerMountAnnotation>().Single(a => !a.Target.Contains("Config.json"));
        Assert.Equal("mydata", volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.False(volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(8081)]
    [InlineData(9007)]
    public void AzureEventHubsWithEmulatorGetsExpectedPort(int? port = null)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubs = builder.AddAzureEventHubs("eventhubs").RunAsEmulator(configureContainer: builder =>
        {
            builder.WithGatewayPort(port);
        });

        Assert.Collection(
            eventHubs.Resource.Annotations.OfType<EndpointAnnotation>(),
            e => Assert.Equal(port, e.Port)
            );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2.3.97-preview")]
    [InlineData("1.0.7")]
    public void AzureEventHubsWithEmulatorGetsExpectedImageTag(string? imageTag)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubs = builder.AddAzureEventHubs("eventhubs");

        eventHubs.RunAsEmulator(container =>
        {
            if (!string.IsNullOrEmpty(imageTag))
            {
                container.WithImageTag(imageTag);
            }
        });

        var containerImageAnnotation = eventHubs.Resource.Annotations.OfType<ContainerImageAnnotation>().FirstOrDefault();
        Assert.NotNull(containerImageAnnotation);

        Assert.Equal(imageTag ?? EventHubsEmulatorContainerImageTags.Tag, containerImageAnnotation.Tag);
        Assert.Equal(EventHubsEmulatorContainerImageTags.Registry, containerImageAnnotation.Registry);
        Assert.Equal(EventHubsEmulatorContainerImageTags.Image, containerImageAnnotation.Image);
    }
}

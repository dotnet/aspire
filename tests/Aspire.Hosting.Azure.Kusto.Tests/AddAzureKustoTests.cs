// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Kusto.Tests;

public class AddAzureKustoTests
{
    [Fact]
    public void AddAzureKustoCluster_ShouldCreateAzureKustoClusterResourceWithCorrectName()
    {
        // Arrange
        const string name = "kusto";
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureKustoCluster(name);

        // Assert
        Assert.NotNull(resourceBuilder);
        Assert.NotNull(resourceBuilder.Resource);
        Assert.Equal(name, resourceBuilder.Resource.Name);
        Assert.IsType<AzureKustoClusterResource>(resourceBuilder.Resource);
    }

    [Theory]
    [InlineData(null, "latest")]
    [InlineData("custom-tag", "custom-tag")]
    public void RunAsEmulator_ShouldConfigureContainerImageWithCorrectTag(string? customTag, string expectedTag)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureKustoCluster("test-kusto").RunAsEmulator(containerBuilder =>
        {
            if (!string.IsNullOrEmpty(customTag))
            {
                containerBuilder.WithImageTag(customTag);
            }
        });

        // Assert
        var containerAnnotation = resourceBuilder.Resource.Annotations.OfType<ContainerImageAnnotation>().SingleOrDefault();
        Assert.NotNull(containerAnnotation);
        Assert.Equal(expectedTag, containerAnnotation.Tag);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(9090)]
    public void RunAsEmulator_ShouldConfigureHttpEndpoint(int? port)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureKustoCluster("kusto").RunAsEmulator(containerBuilder =>
        {
            containerBuilder.WithEndpoint("http", endpoint => endpoint.Port = port);
        });

        // Assert
        var endpointAnnotations = resourceBuilder.Resource.Annotations.OfType<EndpointAnnotation>().ToList();
        var httpEndpoint = endpointAnnotations.SingleOrDefault(e => e.Name == "http");

        Assert.NotNull(httpEndpoint);
        Assert.Equal(port, httpEndpoint.Port);
        Assert.Equal(AzureKustoEmulatorContainerDefaults.DefaultTargetPort, httpEndpoint.TargetPort);
        Assert.Equal("http", httpEndpoint.UriScheme);
    }

    [Fact]
    public void AddAzureKustoCluster_ShouldExcludeFromManifest()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureKustoCluster("kusto");

        // Assert
        var manifestExclusionAnnotation = resourceBuilder.Resource.Annotations.OfType<ManifestPublishingCallbackAnnotation>().SingleOrDefault();
        Assert.Same(ManifestPublishingCallbackAnnotation.Ignore, manifestExclusionAnnotation);
    }

    [Fact]
    public void RunAsEmulator_ShouldAddEmulatorResourceAnnotation()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        const string resourceName = "test-kusto";

        // Act
        var resourceBuilder = builder.AddAzureKustoCluster(resourceName).RunAsEmulator();

        // Assert
        var emulatorAnnotation = resourceBuilder.Resource.Annotations.OfType<EmulatorResourceAnnotation>().SingleOrDefault();
        Assert.NotNull(emulatorAnnotation);
    }

    [Fact]
    public void RunAsEmulator_RespectsConfigurationCallback()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureKustoCluster("kusto").RunAsEmulator(builder =>
        {
            builder.WithAnnotation(new ContainerNameAnnotation() { Name = "custom-kusto-emulator" });
        });

        // Assert
        var annotation = resourceBuilder.Resource.Annotations.OfType<ContainerNameAnnotation>().SingleOrDefault();
        Assert.NotNull(annotation);
        Assert.Equal("custom-kusto-emulator", annotation.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]

    public void AddAzureKustoCluster_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.AddAzureKustoCluster(invalidName!));
    }

    [Theory]
    [InlineData(null)]
    public void AddAzureKustoCluster_WithNullName_ShouldThrowArgumentNullException(string? invalidName)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddAzureKustoCluster(invalidName!));
    }

    [Fact]
    public void AddAzureKustoCluster_WithNullBuilder_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IDistributedApplicationBuilder)null!).AddAzureKustoCluster("test"));
    }

    [Fact]
    public void RunAsEmulator_WithNullBuilder_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IResourceBuilder<AzureKustoClusterResource>)null!).RunAsEmulator());
    }

    [Fact]
    public void AzureKustoClusterResource_ShouldImplementIResourceWithConnectionString()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureKustoCluster("kusto");

        // Assert
        Assert.IsType<IResourceWithConnectionString>(resourceBuilder.Resource, exactMatch: false);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsEmulator_ShouldReturnWhenRunAsEmulator(bool runAsEmulator)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddAzureKustoCluster("test-kusto");

        // Act
        if (runAsEmulator)
        {
            resourceBuilder = resourceBuilder.RunAsEmulator();
        }

        // Assert
        Assert.Equal(runAsEmulator, resourceBuilder.Resource.IsEmulator);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RunAsEmulator_WithVolume_ShouldConfigureVolumeAnnotation(bool isReadOnly)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureKustoCluster("test-kusto").RunAsEmulator(containerBuilder =>
        {
            containerBuilder.WithVolume($"{builder.GetVolumePrefix()}-test-kusto-data", "/data", isReadOnly);
        });

        // Assert
        var volumeAnnotation = resourceBuilder.Resource.Annotations.OfType<ContainerMountAnnotation>().SingleOrDefault();
        Assert.NotNull(volumeAnnotation);
        Assert.Equal($"{builder.GetVolumePrefix()}-test-kusto-data", volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.Equal(isReadOnly, volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RunAsEmulator_WithBindMount_ShouldConfigureBindMountAnnotation(bool isReadOnly)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureKustoCluster("test-kusto").RunAsEmulator(containerBuilder =>
        {
            containerBuilder.WithBindMount("./custom-data", "/data", isReadOnly);
        });

        // Assert
        var mountAnnotation = resourceBuilder.Resource.Annotations.OfType<ContainerMountAnnotation>().SingleOrDefault();
        Assert.NotNull(mountAnnotation);
        Assert.Equal(Path.Combine(builder.AppHostDirectory, "custom-data"), mountAnnotation.Source);
        Assert.Equal("/data", mountAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, mountAnnotation.Type);
        Assert.Equal(isReadOnly, mountAnnotation.IsReadOnly);
    }

    [Fact]
    public void RunAsEmulator_WithBothVolumeAndBindMount_ShouldHaveBothAnnotations()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureKustoCluster("test-kusto").RunAsEmulator(containerBuilder =>
        {
            containerBuilder.WithVolume("volume-data", "/data")
                           .WithBindMount("./config", "/app/config", isReadOnly: true);
        });

        // Assert
        var mountAnnotations = resourceBuilder.Resource.Annotations.OfType<ContainerMountAnnotation>().ToList();
        Assert.Equal(2, mountAnnotations.Count);

        var volumeAnnotation = mountAnnotations.SingleOrDefault(a => a.Type == ContainerMountType.Volume);
        Assert.NotNull(volumeAnnotation);
        Assert.Equal("volume-data", volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);

        var bindMountAnnotation = mountAnnotations.SingleOrDefault(a => a.Type == ContainerMountType.BindMount);
        Assert.NotNull(bindMountAnnotation);
        Assert.Equal("/app/config", bindMountAnnotation.Target);
        Assert.True(bindMountAnnotation.IsReadOnly);
    }

    [Fact]
    public void RunAsEmulator_WithCustomImage_ShouldUseSpecifiedValues()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        const string customRegistry = "custom.registry.com";
        const string customImage = "custom-kusto-image";
        const string customTag = "custom-tag";

        // Act
        var resourceBuilder = builder.AddAzureKustoCluster("test-kusto").RunAsEmulator(containerBuilder =>
        {
            containerBuilder.WithImageRegistry(customRegistry).WithImage(customImage).WithImageTag(customTag);
        });

        // Assert
        var containerImageAnnotation = resourceBuilder.Resource.Annotations.OfType<ContainerImageAnnotation>().SingleOrDefault();
        Assert.NotNull(containerImageAnnotation);
        Assert.Equal(customRegistry, containerImageAnnotation.Registry);
        Assert.Equal(customImage, containerImageAnnotation.Image);
        Assert.Equal(customTag, containerImageAnnotation.Tag);
    }

    [Fact]
    public void RunAsEmulator_WithCustomLifetime_ShouldConfigureLifetimeAnnotation()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureKustoCluster("test-kusto").RunAsEmulator(containerBuilder =>
        {
            containerBuilder.WithLifetime(ContainerLifetime.Persistent);
        });

        // Assert
        var lifetimeAnnotation = resourceBuilder.Resource.Annotations.OfType<ContainerLifetimeAnnotation>().SingleOrDefault();
        Assert.NotNull(lifetimeAnnotation);
        Assert.Equal(ContainerLifetime.Persistent, lifetimeAnnotation.Lifetime);
    }

    [Fact]
    public void AddAzureKustoCluster_ShouldAddHealthCheckAnnotation()
    {
        // Arrange
        const string name = "kusto";
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var kustoServer = builder.AddAzureKustoCluster(name);

        // Assert
        Assert.Single(kustoServer.Resource.Annotations, annotation => annotation is HealthCheckAnnotation hca && hca.Key == $"{name}_check");
    }

    [Fact]
    public void AddDatabase_ShouldAddHealthCheckAnnotation()
    {
        // Arrange
        const string name = "db";
        using var builder = TestDistributedApplicationBuilder.Create();
        var kusto = builder.AddAzureKustoCluster("kusto");

        // Act
        var database = kusto.AddDatabase(name);

        // Assert
        Assert.Single(database.Resource.Annotations, annotation => annotation is HealthCheckAnnotation hca && hca.Key == $"{name}_check");
    }

    [Theory]
    [InlineData(9090)]
    [InlineData(8080)]
    [InlineData(1234)]
    public void WithHttpPort_ShouldSetHttpEndpointPort(int port)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureKustoCluster("kusto")
            .RunAsEmulator(c => c.WithHttpPort(port));

        // Assert
        var endpointAnnotations = resourceBuilder.Resource.Annotations.OfType<EndpointAnnotation>().ToList();
        var httpEndpoint = endpointAnnotations.SingleOrDefault(e => e.Name == "http");

        Assert.NotNull(httpEndpoint);
        Assert.Equal(port, httpEndpoint.Port);
        Assert.Equal(AzureKustoEmulatorContainerDefaults.DefaultTargetPort, httpEndpoint.TargetPort);
        Assert.Equal("http", httpEndpoint.UriScheme);
    }

    [Fact]
    public void WithHttpPort_ShouldThrowArgumentNullException_WhenBuilderIsNull()
    {
        // Arrange
        IResourceBuilder<AzureKustoClusterResource> builder = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => builder.RunAsEmulator(c => c.WithHttpPort(8080)));
        Assert.Equal("builder", exception.ParamName);
    }
}

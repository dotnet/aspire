// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Kusto.Tests;

public class AddKustoTests
{
    [Fact]
    public void AddKusto_ShouldCreateKustoResourceWithCorrectName()
    {
        // Arrange
        const string name = "kusto";
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddKusto(name);

        // Assert
        Assert.NotNull(resourceBuilder);
        Assert.NotNull(resourceBuilder.Resource);
        Assert.Equal(name, resourceBuilder.Resource.Name);
        Assert.IsType<KustoServerResource>(resourceBuilder.Resource);
    }

    [Theory]
    [InlineData(null, "latest")]
    [InlineData("custom-tag", "custom-tag")]
    public void RunAsEmulator_ShouldConfigureContainerImageWithCorrectTag(string? customTag, string expectedTag)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddKusto("test-kusto").RunAsEmulator(configureContainer: containerBuilder =>
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
        var resourceBuilder = builder.AddKusto("kusto").RunAsEmulator(httpPort: port);

        // Assert
        var endpointAnnotations = resourceBuilder.Resource.Annotations.OfType<EndpointAnnotation>().ToList();
        var httpEndpoint = endpointAnnotations.SingleOrDefault(e => e.Name == "http");

        Assert.NotNull(httpEndpoint);
        Assert.Equal(port, httpEndpoint.Port);
        Assert.Equal(KustoEmulatorContainerDefaults.DefaultTargetPort, httpEndpoint.TargetPort);
        Assert.Equal("http", httpEndpoint.UriScheme);
    }

    [Fact]
    public void AddKusto_ShouldExcludeFromManifest()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddKusto("kusto");

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
        var resourceBuilder = builder.AddKusto(resourceName).RunAsEmulator();

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
        var resourceBuilder = builder.AddKusto("kusto").RunAsEmulator(configureContainer: builder =>
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

    public void AddKusto_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.AddKusto(invalidName!));
    }

    [Theory]
    [InlineData(null)]
    public void AddKusto_WithNullName_ShouldThrowArgumentNullException(string? invalidName)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddKusto(invalidName!));
    }

    [Fact]
    public void AddKusto_WithNullBuilder_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IDistributedApplicationBuilder)null!).AddKusto("test"));
    }

    [Fact]
    public void RunAsEmulator_WithNullBuilder_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IResourceBuilder<KustoServerResource>)null!).RunAsEmulator());
    }

    [Fact]
    public void KustoResource_ShouldImplementIResourceWithConnectionString()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddKusto("kusto");

        // Assert
        Assert.IsAssignableFrom<IResourceWithConnectionString>(resourceBuilder.Resource);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsEmulator_ShouldReturnWhenRunAsEmulator(bool runAsEmulator)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddKusto("test-kusto");

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
        var resourceBuilder = builder.AddKusto("test-kusto").RunAsEmulator(configureContainer: containerBuilder =>
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
        var resourceBuilder = builder.AddKusto("test-kusto").RunAsEmulator(configureContainer: containerBuilder =>
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
        var resourceBuilder = builder.AddKusto("test-kusto").RunAsEmulator(configureContainer: containerBuilder =>
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
        var resourceBuilder = builder.AddKusto("test-kusto").RunAsEmulator(configureContainer: containerBuilder =>
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
        var resourceBuilder = builder.AddKusto("test-kusto").RunAsEmulator(configureContainer: containerBuilder =>
        {
            containerBuilder.WithLifetime(ContainerLifetime.Persistent);
        });

        // Assert
        var lifetimeAnnotation = resourceBuilder.Resource.Annotations.OfType<ContainerLifetimeAnnotation>().SingleOrDefault();
        Assert.NotNull(lifetimeAnnotation);
        Assert.Equal(ContainerLifetime.Persistent, lifetimeAnnotation.Lifetime);
    }
}

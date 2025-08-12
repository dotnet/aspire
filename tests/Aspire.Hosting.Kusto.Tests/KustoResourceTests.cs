// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Kusto.Tests;

/// <summary>
/// Tests for <see cref="KustoResource"/> and related functionality.
/// </summary>
public class KustoResourceTests
{
    [Fact]
    public void AddKusto_ShouldCreateKustoResourceWithCorrectDefaults()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var resourceBuilder = builder.AddKusto();

        // Assert
        Assert.NotNull(resourceBuilder);
        Assert.NotNull(resourceBuilder.Resource);
        Assert.Equal("kusto", resourceBuilder.Resource.Name);
        Assert.IsType<KustoResource>(resourceBuilder.Resource);
    }

    [Fact]
    public void AddKusto_ShouldCreateKustoResourceWithCorrectName()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        const string resourceName = "test-kusto";

        // Act
        var resourceBuilder = builder.AddKusto(resourceName);

        // Assert
        Assert.NotNull(resourceBuilder);
        Assert.NotNull(resourceBuilder.Resource);
        Assert.Equal(resourceName, resourceBuilder.Resource.Name);
        Assert.IsType<KustoResource>(resourceBuilder.Resource);
    }

    [Fact]
    public void RunAsEmulator_ShouldConfigureContainerWithCorrectImage()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var resourceBuilder = builder.AddKusto().RunAsEmulator();

        // Assert
        var containerAnnotation = resourceBuilder.Resource.Annotations.OfType<ContainerImageAnnotation>().SingleOrDefault();
        Assert.NotNull(containerAnnotation);
        Assert.Equal("mcr.microsoft.com/azuredataexplorer/kustainer-linux", containerAnnotation.Image);
        Assert.Equal("latest", containerAnnotation.Tag);
    }

    [Fact]
    public void RunAsEmulator_ShouldConfigureHttpEndpoint()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var resourceBuilder = builder.AddKusto().RunAsEmulator();

        // Assert
        List<EndpointAnnotation> endpointAnnotations = [.. resourceBuilder.Resource.Annotations.OfType<EndpointAnnotation>()];
        var httpEndpoint = endpointAnnotations.FirstOrDefault(e => e.Name == "http");

        Assert.NotNull(httpEndpoint);
        Assert.Equal(KustoEmulatorContainerDefaults.DefaultTargetPort, httpEndpoint.TargetPort);
        Assert.Equal("http", httpEndpoint.UriScheme);
    }

    [Fact]
    public void AddKusto_ShouldExcludeFromManifest()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var resourceBuilder = builder.AddKusto();

        // Assert
        var manifestExclusionAnnotation = resourceBuilder.Resource.Annotations.OfType<ManifestPublishingCallbackAnnotation>().SingleOrDefault();
        Assert.NotNull(manifestExclusionAnnotation);
    }

    [Fact]
    public void RunAsEmulator_ShouldAddEmulatorResourceAnnotation()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        const string resourceName = "test-kusto";

        // Act
        var resourceBuilder = builder.AddKusto(resourceName).RunAsEmulator();

        // Assert
        var emulatorAnnotation = resourceBuilder.Resource.Annotations.OfType<EmulatorResourceAnnotation>().SingleOrDefault();
        Assert.NotNull(emulatorAnnotation);
    }

    [Fact]
    public void RunAsEmulator_ShouldUseProvidedPort()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        const int customHttpPort = 9080;

        // Act
        var resourceBuilder = builder.AddKusto().RunAsEmulator(httpPort: customHttpPort);

        // Assert
        List<EndpointAnnotation> endpointAnnotations = [.. resourceBuilder.Resource.Annotations.OfType<EndpointAnnotation>()];

        var httpEndpoint = endpointAnnotations.FirstOrDefault(e => e.Name == "http");
        Assert.NotNull(httpEndpoint);
        Assert.Equal(customHttpPort, httpEndpoint.Port);
    }

    [Fact]
    public void RunAsEmulator_RespectsConfigurationCallback()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var resourceBuilder = builder.AddKusto().RunAsEmulator(configureContainer: builder =>
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
        var builder = DistributedApplication.CreateBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.AddKusto(invalidName!));
    }

    [Theory]
    [InlineData(null)]
    public void AddKusto_WithNullName_ShouldThrowArgumentNullException(string? invalidName)
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

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
        Assert.Throws<ArgumentNullException>(() => ((IResourceBuilder<KustoResource>)null!).RunAsEmulator());
    }

    [Fact]
    public void KustoResource_ShouldImplementIResourceWithConnectionString()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var resourceBuilder = builder.AddKusto();

        // Assert
        Assert.IsAssignableFrom<IResourceWithConnectionString>(resourceBuilder.Resource);
    }

    [Fact]
    public void ConnectionStringExpression_ShouldReturnValidReferenceExpression()
    {
        // Arrange
        var resource = DistributedApplication.CreateBuilder().AddKusto("test-kusto").Resource;

        // Act
        var connectionStringExpression = resource.ConnectionStringExpression;

        // Assert
        Assert.NotNull(connectionStringExpression);
        Assert.Equal("{test-kusto.bindings.http.scheme}://{test-kusto.bindings.http.host}:{test-kusto.bindings.http.port}", connectionStringExpression.ValueExpression);
    }

    [Fact]
    public void IsEmulator_ShouldReturnTrueWhenRunAsEmulator()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var resourceBuilder = builder.AddKusto("test-kusto").RunAsEmulator();
        var resource = resourceBuilder.Resource;

        // Act
        var isEmulator = resource.IsEmulator;

        // Assert
        Assert.True(isEmulator);
    }

    [Fact]
    public void IsEmulator_ShouldReturnFalseWhenNotRunAsEmulator()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var resourceBuilder = builder.AddKusto("test-kusto");
        var resource = resourceBuilder.Resource;

        // Act
        var isEmulator = resource.IsEmulator;

        // Assert
        Assert.False(isEmulator);
    }
}

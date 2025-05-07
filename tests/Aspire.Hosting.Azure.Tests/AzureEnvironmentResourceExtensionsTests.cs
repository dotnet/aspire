#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Azure.Tests;

public class AzureEnvironmentResourceExtensionsTests
{
    [Fact]
    public void AddAzureEnvironment_ShouldAddResourceToBuilder_InPublishMode()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var resourceBuilder = builder.AddAzureEnvironment();

        // Assert
        Assert.NotNull(resourceBuilder);
        Assert.Single(builder.Resources.OfType<AzureEnvironmentResource>());
    }

    [Fact]
    public void AddAzureEnvironment_CalledMultipleTimes_ReturnsSameResource()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var firstBuilder = builder.AddAzureEnvironment();
        var secondBuilder = builder.AddAzureEnvironment();

        // Assert
        Assert.Same(firstBuilder.Resource, secondBuilder.Resource);
        Assert.Single(builder.Resources.OfType<AzureEnvironmentResource>());
    }

    [Fact]
    public void AddAzureEnvironment_InRunMode_DoesNotAddToResources()
    {
        // Arrange
        var builder = CreateBuilder(isRunMode: true);

        // Act
        var resourceBuilder = builder.AddAzureEnvironment();

        // Assert
        Assert.NotNull(resourceBuilder);
        Assert.Empty(builder.Resources.OfType<AzureEnvironmentResource>());
    }

    [Fact]
    public void WithProperties_ShouldConfigureResource()
    {
        // Arrange
        var builder = CreateBuilder(isRunMode: false);
        var resourceBuilder = builder.AddAzureEnvironment();
        var expectedLocation = "westus";

        // Act
        resourceBuilder.WithProperties(resource => resource.Location = expectedLocation);

        // Assert
        var resource = builder.Resources.OfType<AzureEnvironmentResource>().Single();
        Assert.Equal(expectedLocation, resource.Location);
    }

    [Fact]
    public void WithProperties_ChainedCalls_CumulativelyConfigureResource()
    {
        // Arrange
        var builder = CreateBuilder(isRunMode: false);
        var resourceBuilder = builder.AddAzureEnvironment();

        // Act
        resourceBuilder
            .WithProperties(resource => resource.Location = "westus")
            .WithProperties(resource => resource.ResourceGroupName = "TestEnvironment");

        // Assert
        var resource = builder.Resources.OfType<AzureEnvironmentResource>().Single();
        Assert.Equal("westus", resource.Location);
        Assert.Equal("TestEnvironment", resource.ResourceGroupName);
    }

    [Fact]
    public void AddAzureEnvironment_CreatesDefaultName()
    {
        // Arrange
        var builder = CreateBuilder(isRunMode: false);

        // Act
        builder.AddAzureEnvironment();

        // Assert
        var resource = builder.Resources.OfType<AzureEnvironmentResource>().Single();
        Assert.StartsWith("azure-", resource.Name);
    }

    [Fact]
    public void WithLocation_ShouldSetLocationProperty()
    {
        // Arrange
        var builder = CreateBuilder(isRunMode: false);
        var resourceBuilder = builder.AddAzureEnvironment();
        var expectedLocation = "eastus2";

        // Act
        resourceBuilder.WithLocation(expectedLocation);

        // Assert
        var resource = builder.Resources.OfType<AzureEnvironmentResource>().Single();
        Assert.Equal(expectedLocation, resource.Location);
    }

    [Fact]
    public void WithResourceGroup_ShouldSetResourceGroupNameProperty()
    {
        // Arrange
        var builder = CreateBuilder(isRunMode: false);
        var resourceBuilder = builder.AddAzureEnvironment();
        var expectedResourceGroup = "my-resource-group";

        // Act
        resourceBuilder.WithResourceGroup(expectedResourceGroup);

        // Assert
        var resource = builder.Resources.OfType<AzureEnvironmentResource>().Single();
        Assert.Equal(expectedResourceGroup, resource.ResourceGroupName);
    }

    private static IDistributedApplicationBuilder CreateBuilder(bool isRunMode = false)
    {
        var configuration = new ConfigurationManager();
        configuration["AppHost:Sha256"] = "abc123def456";

        var operation = isRunMode ? DistributedApplicationOperation.Run : DistributedApplicationOperation.Publish;
        return TestDistributedApplicationBuilder.Create(operation);
    }
}

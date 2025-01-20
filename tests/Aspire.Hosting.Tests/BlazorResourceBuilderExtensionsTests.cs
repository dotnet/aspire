// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Testing;
using Projects;
using Xunit;

namespace Aspire.Hosting.Tests;

public class BlazorResourceBuilderExtensionsTests
{

    [Fact]
    public async Task AddWebAssemblyClient_WithoutConfigure_InvokesDefaultConfiguration()
    {
        // Arrange
        var distributedApplicationBuilder = await DistributedApplicationTestingBuilder.CreateAsync<TestProject_AppHost>();
        var webApiBuilder = distributedApplicationBuilder.CreateResourceBuilder(new ProjectResource("webapi"));
        var serverBuilder = distributedApplicationBuilder.CreateResourceBuilder(new ProjectResource("serverapp"));

        // Act
        var clientBuilder = serverBuilder.AddWebAssemblyClient<Projects.TestProject_BlazorApp_Client>("clientapp").WithReference(webApiBuilder);

        // Assert
        Assert.NotNull(clientBuilder);
        Assert.IsAssignableFrom<IResourceBuilder<ProjectResource>>(clientBuilder);
    }

    [Fact]
    public async Task AddWebAssemblyClient_WithCustomConfiguration_InvokesConfigurationCallback()
    {
        // Arrange
        var distributedApplicationBuilder = await DistributedApplicationTestingBuilder.CreateAsync<TestProject_AppHost>();
        var serverBuilder = distributedApplicationBuilder.CreateResourceBuilder(new ProjectResource("serverapp"));

        var optionsCaptured = false;

        // Act
        var clientBuilder = serverBuilder.AddWebAssemblyClient<Projects.TestProject_BlazorApp_Client>("clientapp",
            (options, projectMetadata, environment) =>
            {
                optionsCaptured = true;
                Assert.Equal(distributedApplicationBuilder.Environment, environment);
            });

        // Assert
        Assert.NotNull(clientBuilder);
        Assert.True(optionsCaptured);
    }

    [Fact]
    public void AddWebAssemblyClient_HandlesNullResourceBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IResourceBuilder<ProjectResource>? nullResourceBuilder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            nullResourceBuilder!.AddWebAssemblyClient<Projects.TestProject_BlazorApp_Client>("TestWebAssemblyProject"));
    }

    [Fact]
    public async Task AddWebAssemblyClient_EmptyProjectName_ThrowsArgumentException()
    {
        // Arrange
        var distributedApplicationBuilder = await DistributedApplicationTestingBuilder.CreateAsync<TestProject_AppHost>();
        var serverBuilder = distributedApplicationBuilder.CreateResourceBuilder(new ProjectResource("serverapp"));

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            serverBuilder.AddWebAssemblyClient<Projects.TestProject_BlazorApp_Client>(string.Empty));
    }
}

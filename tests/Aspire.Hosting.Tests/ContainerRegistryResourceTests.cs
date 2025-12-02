// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

public class ContainerRegistryResourceTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void AddContainerRegistryWithStrings()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var registry = builder.AddContainerRegistry("docker-hub", "docker.io", "captainsafia");

        Assert.Equal("docker-hub", registry.Resource.Name);

        var containerRegistry = registry.Resource as IContainerRegistry;
        Assert.NotNull(containerRegistry);
        Assert.NotNull(containerRegistry.Name);
        Assert.NotNull(containerRegistry.Endpoint);
        Assert.NotNull(containerRegistry.Repository);
    }

    [Fact]
    public void AddContainerRegistryWithStringsWithoutRepository()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var registry = builder.AddContainerRegistry("acr", "myregistry.azurecr.io");

        Assert.Equal("acr", registry.Resource.Name);

        var containerRegistry = registry.Resource as IContainerRegistry;
        Assert.NotNull(containerRegistry);
        Assert.NotNull(containerRegistry.Name);
        Assert.NotNull(containerRegistry.Endpoint);
        Assert.Null(containerRegistry.Repository);
    }

    [Fact]
    public void AddContainerRegistryWithParameters()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var endpointParam = builder.AddParameter("registry-endpoint");
        var repositoryParam = builder.AddParameter("registry-repo");
        var registry = builder.AddContainerRegistry("my-registry", endpointParam, repositoryParam);

        Assert.Equal("my-registry", registry.Resource.Name);

        var containerRegistry = registry.Resource as IContainerRegistry;
        Assert.NotNull(containerRegistry);
        Assert.NotNull(containerRegistry.Name);
        Assert.NotNull(containerRegistry.Endpoint);
        Assert.NotNull(containerRegistry.Repository);
    }

    [Fact]
    public void AddContainerRegistryWithParametersWithoutRepository()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var endpointParam = builder.AddParameter("registry-endpoint");
        var registry = builder.AddContainerRegistry("my-registry", endpointParam);

        Assert.Equal("my-registry", registry.Resource.Name);

        var containerRegistry = registry.Resource as IContainerRegistry;
        Assert.NotNull(containerRegistry);
        Assert.NotNull(containerRegistry.Name);
        Assert.NotNull(containerRegistry.Endpoint);
        Assert.Null(containerRegistry.Repository);
    }

    [Fact]
    public void AddContainerRegistryWithNullBuilderThrows()
    {
        IDistributedApplicationBuilder builder = null!;

        Assert.Throws<ArgumentNullException>(() => builder.AddContainerRegistry("registry", "docker.io"));
    }

    [Fact]
    public void AddContainerRegistryWithNullNameThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        Assert.Throws<ArgumentNullException>(() => builder.AddContainerRegistry(null!, "docker.io"));
    }

    [Fact]
    public void AddContainerRegistryWithEmptyNameThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        Assert.Throws<ArgumentException>(() => builder.AddContainerRegistry("", "docker.io"));
    }

    [Fact]
    public void AddContainerRegistryWithNullEndpointStringThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        Assert.Throws<ArgumentNullException>(() => builder.AddContainerRegistry("registry", (string)null!));
    }

    [Fact]
    public void AddContainerRegistryWithEmptyEndpointStringThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        Assert.Throws<ArgumentException>(() => builder.AddContainerRegistry("registry", ""));
    }

    [Fact]
    public void AddContainerRegistryWithNullEndpointParameterThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        Assert.Throws<ArgumentNullException>(() => builder.AddContainerRegistry("registry", (IResourceBuilder<ParameterResource>)null!));
    }

    [Fact]
    public void ContainerRegistryResourceIsAddedToModel()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        builder.AddContainerRegistry("docker-hub", "docker.io", "myuser");

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var registry = Assert.Single(appModel.Resources.OfType<ContainerRegistryResource>());
        Assert.Equal("docker-hub", registry.Name);
    }

    [Fact]
    public async Task ContainerRegistryEndpointExpressionResolves()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var registry = builder.AddContainerRegistry("docker-hub", "docker.io", "captainsafia");

        var containerRegistry = registry.Resource as IContainerRegistry;
        Assert.NotNull(containerRegistry);

        var endpointValue = await containerRegistry.Endpoint.GetValueAsync(default);
        Assert.Equal("docker.io", endpointValue);
    }

    [Fact]
    public async Task ContainerRegistryRepositoryExpressionResolves()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var registry = builder.AddContainerRegistry("docker-hub", "docker.io", "captainsafia");

        var containerRegistry = registry.Resource as IContainerRegistry;
        Assert.NotNull(containerRegistry);
        Assert.NotNull(containerRegistry.Repository);

        var repositoryValue = await containerRegistry.Repository.GetValueAsync(default);
        Assert.Equal("captainsafia", repositoryValue);
    }

    [Fact]
    public async Task ContainerRegistryNameExpressionResolves()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var registry = builder.AddContainerRegistry("docker-hub", "docker.io", "captainsafia");

        var containerRegistry = registry.Resource as IContainerRegistry;
        Assert.NotNull(containerRegistry);

        var nameValue = await containerRegistry.Name.GetValueAsync(default);
        Assert.Equal("docker-hub", nameValue);
    }

    [Fact]
    public async Task ContainerRegistryWithParameterEndpointResolves()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        builder.Configuration["Parameters:registry-endpoint"] = "ghcr.io";

        var endpointParam = builder.AddParameter("registry-endpoint");
        var registry = builder.AddContainerRegistry("ghcr", endpointParam);

        var containerRegistry = registry.Resource as IContainerRegistry;
        Assert.NotNull(containerRegistry);

        var endpointValue = await containerRegistry.Endpoint.GetValueAsync(default);
        Assert.Equal("ghcr.io", endpointValue);
    }

    [Fact]
    public async Task ContainerRegistryWithParameterRepositoryResolves()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        builder.Configuration["Parameters:registry-endpoint"] = "ghcr.io";
        builder.Configuration["Parameters:registry-repo"] = "captainsafia/my-repo";

        var endpointParam = builder.AddParameter("registry-endpoint");
        var repositoryParam = builder.AddParameter("registry-repo");
        var registry = builder.AddContainerRegistry("ghcr", endpointParam, repositoryParam);

        var containerRegistry = registry.Resource as IContainerRegistry;
        Assert.NotNull(containerRegistry);
        Assert.NotNull(containerRegistry.Repository);

        var repositoryValue = await containerRegistry.Repository.GetValueAsync(default);
        Assert.Equal("captainsafia/my-repo", repositoryValue);
    }

    [Fact]
    public void ContainerRegistryResourceImplementsIContainerRegistry()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var registry = builder.AddContainerRegistry("docker-hub", "docker.io", "myuser");

        Assert.IsAssignableFrom<IContainerRegistry>(registry.Resource);
    }

    [Fact]
    public void MultipleContainerRegistriesCanBeAdded()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        builder.AddContainerRegistry("docker-hub", "docker.io", "user1");
        builder.AddContainerRegistry("ghcr", "ghcr.io", "user2/repo");
        builder.AddContainerRegistry("acr", "myregistry.azurecr.io");

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var registries = appModel.Resources.OfType<ContainerRegistryResource>().ToList();
        Assert.Equal(3, registries.Count);
        Assert.Contains(registries, r => r.Name == "docker-hub");
        Assert.Contains(registries, r => r.Name == "ghcr");
        Assert.Contains(registries, r => r.Name == "acr");
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class AzureFunctionsTests
{
    [Fact]
    public void AddAzureFunctionsProject_Works()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddAzureFunctionsProject<TestProject>("funcapp");

        // Assert that default storage resource is configured
        Assert.Contains(builder.Resources, resource =>
            resource is AzureStorageResource && resource.Name.StartsWith(AzureFunctionsProjectResourceExtensions.DefaultAzureFunctionsHostStorageName));
        // Assert that custom project resource type is configured
        Assert.Contains(builder.Resources, resource =>
            resource is AzureFunctionsProjectResource && resource.Name == "funcapp");
    }

    [Fact]
    public void AddAzureFunctionsProject_WiresUpHttpEndpointCorrectly_WhenPortArgumentIsProvided()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddAzureFunctionsProject<TestProject>("funcapp");

        // Assert that the EndpointAnnotation is configured correctly
        var functionsResource = Assert.Single(builder.Resources.OfType<AzureFunctionsProjectResource>());
        Assert.True(functionsResource.TryGetLastAnnotation<EndpointAnnotation>(out var endpointAnnotation));
        Assert.Equal(7071, endpointAnnotation.Port);
        Assert.Equal(7071, endpointAnnotation.TargetPort);
        Assert.False(endpointAnnotation.IsProxied);
    }

    [Fact]
    public void AddAzureFunctionsProject_WiresUpHttpEndpointCorrectly_WhenPortArgumentIsNotProvided()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddAzureFunctionsProject<TestProjectWithoutPortArgument>("funcapp");

        // Assert that the EndpointAnnotation uses the first port defined in launch settings when
        // there are multiple
        var functionsResource = Assert.Single(builder.Resources.OfType<AzureFunctionsProjectResource>());
        Assert.True(functionsResource.TryGetLastAnnotation<EndpointAnnotation>(out var endpointAnnotation));
        Assert.Null(endpointAnnotation.Port);
        Assert.Null(endpointAnnotation.TargetPort);
        Assert.True(endpointAnnotation.IsProxied);
    }

    [Fact]
    public void AddAzureFunctionsProject_WiresUpHttpEndpointCorrectly_WhenMultiplePortArgumentsProvided()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddAzureFunctionsProject<TestProjectWithMultiplePorts>("funcapp");

        // Assert that the EndpointAnnotation is configured correctly
        var functionsResource = Assert.Single(builder.Resources.OfType<AzureFunctionsProjectResource>());
        Assert.True(functionsResource.TryGetLastAnnotation<EndpointAnnotation>(out var endpointAnnotation));
        Assert.Equal(7072, endpointAnnotation.Port);
        Assert.Equal(7072, endpointAnnotation.TargetPort);
        Assert.False(endpointAnnotation.IsProxied);
    }

    [Fact]
    public void AddAzureFunctionsProject_WiresUpHttpEndpointCorrectly_WhenPortArgumentIsMalformed()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddAzureFunctionsProject<TestProjectWithMalformedPort>("funcapp");

        // Assert that the EndpointAnnotation is configured correctly
        var functionsResource = Assert.Single(builder.Resources.OfType<AzureFunctionsProjectResource>());
        Assert.True(functionsResource.TryGetLastAnnotation<EndpointAnnotation>(out var endpointAnnotation));
        Assert.Null(endpointAnnotation.Port);
        Assert.Null(endpointAnnotation.TargetPort);
        Assert.True(endpointAnnotation.IsProxied);
    }

    [Fact]
    public void AddAzureFunctionsProject_WiresUpHttpEndpointCorrectly_WhenPortArgumentIsPartial()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddAzureFunctionsProject<TestProjectWithPartialPort>("funcapp");

        // Assert that the EndpointAnnotation is configured correctly
        var functionsResource = Assert.Single(builder.Resources.OfType<AzureFunctionsProjectResource>());
        Assert.True(functionsResource.TryGetLastAnnotation<EndpointAnnotation>(out var endpointAnnotation));
        Assert.Null(endpointAnnotation.Port);
        Assert.Null(endpointAnnotation.TargetPort);
        Assert.True(endpointAnnotation.IsProxied);
    }

    [Fact]
    public void AddAzureFunctionsProject_GeneratesUniqueDefaultHostStorageResourceName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddAzureFunctionsProject<TestProjectWithMalformedPort>("funcapp");

        // Assert that the default storage resource is unique
        var storageResources = Assert.Single(builder.Resources.OfType<AzureStorageResource>());
        Assert.NotEqual(AzureFunctionsProjectResourceExtensions.DefaultAzureFunctionsHostStorageName, storageResources.Name);
        Assert.StartsWith(AzureFunctionsProjectResourceExtensions.DefaultAzureFunctionsHostStorageName, storageResources.Name);
    }

    [Fact]
    [RequiresDocker]
    public async Task AddAzureFunctionsProject_RemoveDefaultHostStorageWhenUseHostStorageIsUsed()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("my-own-storage").RunAsEmulator();
        builder.AddAzureFunctionsProject<TestProjectWithMalformedPort>("funcapp")
            .WithHostStorage(storage);

        using var host = builder.Build();
        await host.StartAsync();

        // Assert that the default storage resource is not present
        var model = host.Services.GetRequiredService<DistributedApplicationModel>();
        Assert.DoesNotContain(model.Resources.OfType<AzureStorageResource>(),
            r => r.Name.StartsWith(AzureFunctionsProjectResourceExtensions.DefaultAzureFunctionsHostStorageName));
        var storageResource = Assert.Single(model.Resources.OfType<AzureStorageResource>());
        Assert.Equal("my-own-storage", storageResource.Name);

        await host.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task AddAzureFunctionsProject_WorksWithMultipleProjects()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddAzureFunctionsProject<TestProject>("funcapp");
        builder.AddAzureFunctionsProject<TestProject>("funcapp2");

        using var host = builder.Build();
        await host.StartAsync();

        // Assert that the default storage resource is not present
        var model = host.Services.GetRequiredService<DistributedApplicationModel>();
        Assert.Single(model.Resources.OfType<AzureStorageResource>(),
            r => r.Name.StartsWith(AzureFunctionsProjectResourceExtensions.DefaultAzureFunctionsHostStorageName));

        await host.StopAsync();
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "some-path";

        public LaunchSettings LaunchSettings => new()
        {
            Profiles = new Dictionary<string, LaunchProfile>
            {
                ["funcapp"] = new()
                {
                    CommandLineArgs = "--port 7071",
                    LaunchBrowser = false,
                }
            }
        };
    }

    private sealed class TestProjectWithMalformedPort : IProjectMetadata
    {
        public string ProjectPath => "some-path";

        public LaunchSettings LaunchSettings => new()
        {
            Profiles = new Dictionary<string, LaunchProfile>
            {
                ["funcapp"] = new()
                {
                    CommandLineArgs = "--port 70b1",
                    LaunchBrowser = false,
                }
            }
        };
    }

    private sealed class TestProjectWithPartialPort : IProjectMetadata
    {
        public string ProjectPath => "some-path";

        public LaunchSettings LaunchSettings => new()
        {
            Profiles = new Dictionary<string, LaunchProfile>
            {
                ["funcapp"] = new()
                {
                    CommandLineArgs = "--port",
                    LaunchBrowser = false,
                }
            }
        };
    }

    private sealed class TestProjectWithoutPortArgument : IProjectMetadata
    {
        public string ProjectPath => "some-path";

        public LaunchSettings LaunchSettings => new()
        {
            Profiles = new Dictionary<string, LaunchProfile>
            {
                ["funcapp"] = new()
                {
                    LaunchBrowser = false,
                }
            }
        };
    }

    private sealed class TestProjectWithMultiplePorts : IProjectMetadata
    {
        public string ProjectPath => "some-path";

        public LaunchSettings LaunchSettings => new()
        {
            Profiles = new Dictionary<string, LaunchProfile>
            {
                ["funcapp"] = new()
                {
                    CommandLineArgs = "--port 7072 --port 7071",
                    LaunchBrowser = false,
                }
            }
        };
    }
}

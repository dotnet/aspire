// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
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
            resource is AzureStorageResource && resource.Name == AzureFunctionsProjectResourceExtensions.DefaultAzureFunctionsHostStorageName);
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

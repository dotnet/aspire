// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class AzureFunctionsTests(ITestOutputHelper testOutputHelper)
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

    [Theory]
    [RequiresDocker]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddAzureFunctionsProject_LogsWhenUsingPreExistingDefaultStorage(bool defaultHostStorageAlreadyExists)
    {
        AzureFunctionsProjectResourceExtensions.s_isFirstInvocation = true;
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var testSink = new TestSink();
        var loggerFactory = CreateLoggerFactory(testOutputHelper, testSink);
        builder.Services.AddSingleton(loggerFactory);

        if (defaultHostStorageAlreadyExists)
        {
            builder.AddAzureStorage(AzureFunctionsProjectResourceExtensions.DefaultAzureFunctionsHostStorageName);
        }
        builder.AddAzureFunctionsProject<TestProjectWithPartialPort>("funcapp");

        var host = builder.Build();
        await host.StartAsync();

        Assert.Equal(defaultHostStorageAlreadyExists, testSink.Writes.SingleOrDefault(write =>
            write.LogLevel == LogLevel.Warning &&
            write.LoggerName == AzureFunctionsProjectResourceExtensions.LogCategoryName &&
            write.Message is { } message &&
            message.Contains("Found existing default Storage resource 'azFuncHostStorage' for Azure Functions project")) != null);

        await host.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task AddAzureFunctionsProject_DoesNotLogWhenMultipleProjectsRegistered()
    {
        AzureFunctionsProjectResourceExtensions.s_isFirstInvocation = true;
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var testSink = new TestSink();
        var loggerFactory = CreateLoggerFactory(testOutputHelper, testSink);
        builder.Services.AddSingleton(loggerFactory);

        builder.AddAzureFunctionsProject<TestProjectWithPartialPort>("funcapp");
        builder.AddAzureFunctionsProject<TestProjectWithPartialPort>("funcapp2");
        builder.AddAzureFunctionsProject<TestProjectWithPartialPort>("funcapp3");

        var host = builder.Build();
        await host.StartAsync();

        Assert.DoesNotContain(testSink.Writes, write =>
            write.LogLevel == LogLevel.Warning &&
            write.LoggerName == AzureFunctionsProjectResourceExtensions.LogCategoryName &&
            write.Message is { } message &&
            message.Contains("Found existing default Storage resource 'azFuncHostStorage' for Azure Functions projects"));

        await host.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task AddAzureFunctionsProject_LogsWhenHostStorageConfiguredWithMultipleProjects()
    {
        AzureFunctionsProjectResourceExtensions.s_isFirstInvocation = true;
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var testSink = new TestSink();
        var loggerFactory = CreateLoggerFactory(testOutputHelper, testSink);
        builder.Services.AddSingleton(loggerFactory);

        builder.AddAzureStorage(AzureFunctionsProjectResourceExtensions.DefaultAzureFunctionsHostStorageName);
        builder.AddAzureFunctionsProject<TestProjectWithPartialPort>("funcapp");
        builder.AddAzureFunctionsProject<TestProjectWithPartialPort>("funcapp2");
        builder.AddAzureFunctionsProject<TestProjectWithPartialPort>("funcapp3");

        var host = builder.Build();
        await host.StartAsync();

        Assert.Single(testSink.Writes, write =>
            write.LogLevel == LogLevel.Warning &&
            write.LoggerName == AzureFunctionsProjectResourceExtensions.LogCategoryName &&
            write.Message is { } message &&
            message.Contains("Found existing default Storage resource 'azFuncHostStorage' for Azure Functions projects"));

        await host.StopAsync();
    }

    public static ILoggerFactory CreateLoggerFactory(ITestOutputHelper testOutputHelper, ITestSink? testSink = null)
    {
        return LoggerFactory.Create(builder =>
        {
            builder.AddXunit(testOutputHelper, LogLevel.Trace, DateTimeOffset.UtcNow);
            builder.SetMinimumLevel(LogLevel.Trace);
            if (testSink is not null)
            {
                builder.AddProvider(new TestLoggerProvider(testSink));
            }
        });
    }

    private sealed class TestProjectNoStart : IProjectMetadata
    {
        public string ProjectPath => "some-path";
        public LaunchSettings LaunchSettings => new();

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

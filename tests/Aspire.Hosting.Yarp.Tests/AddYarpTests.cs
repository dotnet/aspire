// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Yarp.Tests;

public class AddYarpTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void VerifyYarpResourceWithTargetPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var yarp = builder.AddYarp("yarp");
        using var app = builder.Build();

        var resource = Assert.Single(builder.Resources.OfType<YarpResource>());
        var endpoint = Assert.Single(resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(5000, endpoint.TargetPort);
    }

    [Fact]
    public async Task VerifyRunEnvVariablesAreSet()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var yarp = builder.AddYarp("yarp");

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(yarp.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Contains("OTEL_EXPORTER_OTLP_ENDPOINT", env);
        Assert.Contains("OTEL_EXPORTER_OTLP_PROTOCOL", env);
        Assert.Contains("ASPNETCORE_ENVIRONMENT", env);

        var value = Assert.Contains("YARP_UNSAFE_OLTP_CERT_ACCEPT_ANY_SERVER_CERTIFICATE", env);
        Assert.Equal("true", value);
    }

    [Fact]
    public async Task VerifyPublishEnvVariablesAreSet()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var yarp = builder.AddYarp("yarp");

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(yarp.Resource, DistributedApplicationOperation.Publish, TestServiceProvider.Instance);

        Assert.DoesNotContain("OTEL_EXPORTER_OTLP_ENDPOINT", env);
        Assert.DoesNotContain("OTEL_EXPORTER_OTLP_PROTOCOL", env);
        Assert.Contains("ASPNETCORE_ENVIRONMENT", env);

        Assert.DoesNotContain("YARP_UNSAFE_OLTP_CERT_ACCEPT_ANY_SERVER_CERTIFICATE", env);
    }

    [Fact]
    public async Task VerifyWithStaticFilesAddsEnvironmentVariable()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var yarp = builder.AddYarp("yarp").WithStaticFiles();

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(yarp.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        var value = Assert.Contains("YARP_ENABLE_STATIC_FILES", env);
        Assert.Equal("true", value);
    }

    [Fact]
    public async Task VerifyWithStaticFilesWorksInPublishOperation()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var yarp = builder.AddYarp("yarp").WithStaticFiles();

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(yarp.Resource, DistributedApplicationOperation.Publish, TestServiceProvider.Instance);

        var value = Assert.Contains("YARP_ENABLE_STATIC_FILES", env);
        Assert.Equal("true", value);
    }

    [Fact]
    public async Task VerifyWithStaticFilesBindMountAddsEnvironmentVariable()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        using var tempDir = new TempDirectory();
        
        var yarp = builder.AddYarp("yarp").WithStaticFiles(tempDir.Path);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(yarp.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        var value = Assert.Contains("YARP_ENABLE_STATIC_FILES", env);
        Assert.Equal("true", value);
    }

    [Fact]
    public async Task VerifyWithStaticFilesBindMountWorksInPublishOperation()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        using var tempDir = new TempDirectory();
        
        var yarp = builder.AddYarp("yarp").WithStaticFiles(tempDir.Path);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(yarp.Resource, DistributedApplicationOperation.Publish, TestServiceProvider.Instance);

        var value = Assert.Contains("YARP_ENABLE_STATIC_FILES", env);
        Assert.Equal("true", value);
    }

    [Fact]
    public void VerifyWithStaticFilesBindMountAddsContainerFileSystemAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var tempDir = new TempDirectory();
        
        var yarp = builder.AddYarp("yarp").WithStaticFiles(tempDir.Path);

        var annotation = Assert.Single(yarp.Resource.Annotations.OfType<ContainerFileSystemCallbackAnnotation>());
        Assert.Equal("/wwwroot", annotation.DestinationPath);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task VerifyRunEnvVariablesAreSet(bool containerCertificateSupport)
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        // Report that developer certificates won't support container scenarios
        var testProvider = new TestServiceProvider();
        testProvider.AddService<IDeveloperCertificateService>(new TestDeveloperCertificateService(
            new List<X509Certificate2>(),
            containerCertificateSupport,
            trustCertificate: true));

        var yarp = builder.AddYarp("yarp");

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(yarp.Resource, DistributedApplicationOperation.Run, testProvider);

        Assert.Contains("OTEL_EXPORTER_OTLP_ENDPOINT", env);
        Assert.Contains("OTEL_EXPORTER_OTLP_PROTOCOL", env);
        Assert.Contains("ASPNETCORE_ENVIRONMENT", env);

        if (containerCertificateSupport)
        {
            Assert.DoesNotContain("YARP_UNSAFE_OLTP_CERT_ACCEPT_ANY_SERVER_CERTIFICATE", env);
        }
        else
        {
            var value = Assert.Contains("YARP_UNSAFE_OLTP_CERT_ACCEPT_ANY_SERVER_CERTIFICATE", env);
            Assert.Equal("true", value);
        }
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

        // Yarp requires an IDeveloperCertificateService in run mode when building it's environment variables.
        var testProvider = new TestServiceProvider();
        testProvider.AddService<IDeveloperCertificateService>(new TestDeveloperCertificateService(
            new List<X509Certificate2>(),
            supportsContainerTrust: false,
            trustCertificate: true));

        var yarp = builder.AddYarp("yarp").WithStaticFiles();

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(yarp.Resource, DistributedApplicationOperation.Run, testProvider);

        var value = Assert.Contains("YARP_ENABLE_STATIC_FILES", env);
        Assert.Equal("true", value);
    }

    [Fact]
    public async Task VerifyWithStaticFilesWorksInPublishOperation()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // Yarp requires an IDeveloperCertificateService in run mode when building it's environment variables.
        var testProvider = new TestServiceProvider();
        testProvider.AddService<IDeveloperCertificateService>(new TestDeveloperCertificateService(
            new List<X509Certificate2>(),
            supportsContainerTrust: false,
            trustCertificate: true));

        var yarp = builder.AddYarp("yarp").WithStaticFiles();

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(yarp.Resource, DistributedApplicationOperation.Publish, testProvider);

        var value = Assert.Contains("YARP_ENABLE_STATIC_FILES", env);
        Assert.Equal("true", value);
    }

    [Fact]
    public async Task VerifyWithStaticFilesBindMountAddsEnvironmentVariable()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        // Yarp requires an IDeveloperCertificateService in run mode when building it's environment variables.
        var testProvider = new TestServiceProvider();
        testProvider.AddService<IDeveloperCertificateService>(new TestDeveloperCertificateService(
            new List<X509Certificate2>(),
            supportsContainerTrust: false,
            trustCertificate: true));

        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarp("yarp").WithStaticFiles(tempDir.Path);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(yarp.Resource, DistributedApplicationOperation.Run, testProvider);

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
    public void VerifyWithStaticFilesBindMountAddsContainerFileSystemAnnotationInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarp("yarp").WithStaticFiles(tempDir.Path);

        var annotation = Assert.Single(yarp.Resource.Annotations.OfType<ContainerFileSystemCallbackAnnotation>());
        Assert.Equal("/wwwroot", annotation.DestinationPath);
    }

    [Fact]
    public void VerifyWithStaticFilesAddsDockerfileBuildAnnotationInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarp("yarp").WithStaticFiles(tempDir.Path);

        var annotation = Assert.Single(yarp.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.NotNull(annotation.DockerfileFactory);
        Assert.Contains(tempDir.Path, annotation.ContextPath);
    }

    [Fact]
    public async Task VerifyWithStaticFilesGeneratesCorrectDockerfileInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        using var tempDir = new TempDirectory();

        var yarp = builder.AddYarp("yarp").WithStaticFiles(tempDir.Path);

        var annotation = Assert.Single(yarp.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.NotNull(annotation.DockerfileFactory);

        var context = new DockerfileFactoryContext
        {
            Resource = yarp.Resource,
            Services = TestServiceProvider.Instance,
            CancellationToken = CancellationToken.None
        };

        var dockerfile = await annotation.DockerfileFactory(context);

        Assert.Contains("FROM", dockerfile);
        Assert.Contains("dotnet/nightly/yarp:2.3.0-preview.4", dockerfile);
        Assert.Contains("AS yarp", dockerfile);
        Assert.Contains("WORKDIR /app", dockerfile);
        Assert.Contains("COPY . /app/wwwroot", dockerfile);
    }
}

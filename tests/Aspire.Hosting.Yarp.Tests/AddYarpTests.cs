// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Xunit;

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
}

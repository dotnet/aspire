// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests;

public class OtlpEndpointResourceTests
{
    [Fact]
    public void AddOtlpEndpointCreatesResourceWithCorrectName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var otlp = builder.AddOtlpEndpoint("my-otlp");

        Assert.Equal("my-otlp", otlp.Resource.Name);
        Assert.IsType<OtlpEndpointResource>(otlp.Resource);
    }

    [Fact]
    public void AddOtlpEndpointCreatesResourceWithEndpoints()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var otlp = builder.AddOtlpEndpoint("my-otlp");

        var endpoints = otlp.Resource.Annotations.OfType<EndpointAnnotation>().ToList();
        Assert.NotEmpty(endpoints);
        Assert.Contains(endpoints, e => e.Name == "otlp");
    }

    [Fact]
    public void AddOtlpEndpointWithCustomEndpoints()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var otlp = builder.AddOtlpEndpoint("my-otlp", 
            grpcEndpoint: "http://grafana:4317",
            httpEndpoint: "http://grafana:4318");

        var endpoints = otlp.Resource.Annotations.OfType<EndpointAnnotation>().ToList();
        Assert.NotEmpty(endpoints);
    }

    [Fact]
    public async Task OtlpEndpointResourceHasConnectionString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var otlp = builder.AddOtlpEndpoint("my-otlp");

        Assert.NotNull(otlp.Resource.ConnectionStringExpression);
        
        // The connection string should be resolvable
        var connectionString = await otlp.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.NotNull(connectionString);
    }

    [Fact]
    public async Task WithReferenceToOtlpEndpointConfiguresEnvironmentVariables()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "http://localhost:8889";

        var otlp = builder.AddOtlpEndpoint("my-otlp");
        var container = builder.AddResource(new ContainerResource("testSource"))
            .WithReference(otlp);

        using var app = builder.Build();

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            container.Resource,
            serviceProvider: app.Services
        );

        Assert.True(config.ContainsKey("OTEL_EXPORTER_OTLP_ENDPOINT"));
        Assert.True(config.ContainsKey("OTEL_EXPORTER_OTLP_PROTOCOL"));
    }

    [Fact]
    public async Task WithReferenceToOtlpEndpointSupportsProtocolSelection()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.Configuration["ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"] = "http://localhost:8890";

        var otlp = builder.AddOtlpEndpoint("my-otlp");
        var container = builder.AddResource(new ContainerResource("testSource"))
            .WithReference(otlp, OtlpProtocol.HttpProtobuf);

        using var app = builder.Build();

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            container.Resource,
            serviceProvider: app.Services
        );

        Assert.Equal("http/protobuf", config["OTEL_EXPORTER_OTLP_PROTOCOL"]);
    }

    [Fact]
    public void OtlpEndpointResourceImplementsIResourceWithEndpoints()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var otlp = builder.AddOtlpEndpoint("my-otlp");

        Assert.IsAssignableFrom<IResourceWithEndpoints>(otlp.Resource);
    }

    [Fact]
    public void OtlpEndpointResourceImplementsIResourceWithConnectionString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var otlp = builder.AddOtlpEndpoint("my-otlp");

        Assert.IsAssignableFrom<IResourceWithConnectionString>(otlp.Resource);
    }

    [Fact]
    public void OtlpEndpointResourceHasGrpcAndHttpEndpoints()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var otlp = builder.AddOtlpEndpoint("my-otlp");

        // Check that the resource exposes both gRPC and HTTP endpoint references
        Assert.NotNull(otlp.Resource.GrpcEndpoint);
        Assert.NotNull(otlp.Resource.HttpEndpoint);
        Assert.NotNull(otlp.Resource.PrimaryEndpoint);
    }

    [Fact]
    public async Task WithReferenceAddsOtlpExporterAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var otlp = builder.AddOtlpEndpoint("my-otlp");
        var container = builder.AddResource(new ContainerResource("testSource"))
            .WithReference(otlp);

        // Verify the annotation was added
        var annotation = container.Resource.Annotations.OfType<OtlpExporterAnnotation>().FirstOrDefault();
        Assert.NotNull(annotation);
    }

    [Fact]
    public async Task WithReferenceConfiguresDevelopmentSettings()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "http://localhost:8889";

        var otlp = builder.AddOtlpEndpoint("my-otlp");
        var container = builder.AddResource(new ContainerResource("testSource"))
            .WithReference(otlp);

        using var app = builder.Build();

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            container.Resource,
            serviceProvider: app.Services
        );

        // Verify development-specific settings are applied
        Assert.Equal("1000", config["OTEL_BLRP_SCHEDULE_DELAY"]);
        Assert.Equal("1000", config["OTEL_BSP_SCHEDULE_DELAY"]);
        Assert.Equal("1000", config["OTEL_METRIC_EXPORT_INTERVAL"]);
        Assert.Equal("always_on", config["OTEL_TRACES_SAMPLER"]);
        Assert.Equal("trace_based", config["OTEL_METRICS_EXEMPLAR_FILTER"]);
    }
}

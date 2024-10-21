// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;

public class ResourceWithProbeTests
{
    [Fact]
    public void ProbesWithSameTypeThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            const string endpointName = "http";
            var appBuilder = DistributedApplication.CreateBuilder();
            var resource = appBuilder.AddResource(new CustomResourceWithProbes("myResouce"));
            resource.WithHttpsEndpoint(3000, 3000, endpointName);
            resource.WithProbe(endpointName, ProbeType.Readiness, "/health");
            resource.WithProbe(endpointName, ProbeType.Readiness, "/ready");
        });

        Assert.Equal("Probe with type 'Readiness' already exists", ex.Message);
    }

    [Fact]
    public void CreatesAnnotation()
    {
        const string endpointName = "http";
        var appBuilder = DistributedApplication.CreateBuilder();
        var resource = appBuilder.AddResource(new CustomResourceWithProbes("myResouce"));
        resource.WithHttpsEndpoint(3000, 3000, endpointName);
        resource.WithProbe(endpointName, ProbeType.Startup, "/health");

        var annotations = resource.Resource.Annotations.OfType<ProbeAnnotation>().ToArray();

        Assert.Single(annotations);
    }

    [Fact]
    public void CreatesMultipleProbeTypes()
    {
        const string endpointName = "myEndpoint";
        var appBuilder = DistributedApplication.CreateBuilder();
        var resource = appBuilder.AddResource(new CustomResourceWithProbes("myResouce"));
        resource.WithHttpsEndpoint(3000, 3000, endpointName);
        resource.WithLivenessProbe(endpointName, "/health");
        resource.WithReadinessProbe(endpointName, "/ready");

        var annotations = resource.Resource.Annotations.OfType<ProbeAnnotation>().ToArray();

        Assert.Equal(2, annotations.Length);
    }
}

/// <summary>
/// Temporary dummy resource to test the health probes. TODO: remove when one the actual resources implements <see cref="IResourceWithProbes"/>
/// </summary>
internal sealed class CustomResourceWithProbes : ContainerResource, IResourceWithProbes
{
    public CustomResourceWithProbes(string name) : base(name)
    {
    }
}

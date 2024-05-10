// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;

public class WithHealthProbeTests
{

    [Fact]
    public void ProbesWithSameTypeThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            const string endpointName = "myEndpoint";
            var appBuilder = DistributedApplication.CreateBuilder();
            var resource = appBuilder.AddResource(new CustomResource("myResouce"));
            resource.WithHttpsEndpoint(3000, endpointName);
            resource.WithHealthProbe(HealthProbeType.Readiness, resource.GetEndpoint(endpointName), "/health");
            resource.WithHealthProbe(HealthProbeType.Readiness, resource.GetEndpoint(endpointName), "/ready");
        });

        Assert.Equal("Probe with type 'Readiness' already exists", ex.Message);
    }

    [Fact]
    public void CreatesAnnotation()
    {
        const string endpointName = "myEndpoint";
        var appBuilder = DistributedApplication.CreateBuilder();
        var resource = appBuilder.AddResource(new CustomResource("myResouce"));
        resource.WithHttpsEndpoint(3000, endpointName);
        resource.WithHealthProbe(HealthProbeType.Startup, resource.GetEndpoint(endpointName), "/health");

        var annotations = resource.Resource.Annotations.OfType<HealthProbeAnnotation>().ToArray();

        Assert.Single(annotations);
    }

    [Fact]
    public void CreatesMultipleProbeTypes()
    {
        const string endpointName = "myEndpoint";
        var appBuilder = DistributedApplication.CreateBuilder();
        var resource = appBuilder.AddResource(new CustomResource("myResouce"));
        resource.WithHttpsEndpoint(3000, endpointName);
        resource.WithHealthProbe(HealthProbeType.Startup, resource.GetEndpoint(endpointName), "/health");
        resource.WithHealthProbe(HealthProbeType.Readiness, resource.GetEndpoint(endpointName), "/ready");

        var annotations = resource.Resource.Annotations.OfType<HealthProbeAnnotation>().ToArray();

        Assert.Equal(2, annotations.Length);
    }

}

/// <summary>
/// Temporary dummy resource to test the health probes. TODO: remove when one the actual resources implements <see cref="IResourceWithHealthProbes"/>
/// </summary>
file sealed class CustomResource : ContainerResource, IResourceWithHealthProbes
{
    public CustomResource(string name) : base(name)
    {
    }
}

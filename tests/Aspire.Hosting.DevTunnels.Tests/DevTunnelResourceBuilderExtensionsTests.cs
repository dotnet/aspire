// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.DevTunnels.Tests;

public class DevTunnelResourceBuilderExtensionsTests
{
    [Fact]
    public async Task WithReference_InjectsServiceDiscoveryEnvironmentVariablesWhenReferencingOtherResourcesViaTheTunnel()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var target = builder.AddProject<ProjectA>("target")
            .WithHttpsEndpoint();
        var tunnel = builder.AddDevTunnel("tunnel")
            .WithReference(target);
        var consumer = builder.AddResource(new TestResource("consumer"))
            .WithReference(target, tunnel);

        var tunnelPort = tunnel.Resource.Ports.FirstOrDefault();
        Assert.NotNull(tunnelPort);

        tunnelPort.TunnelEndpointAnnotation.AllocatedEndpoint = new(tunnelPort.TunnelEndpointAnnotation, "test123.devtunnels.ms", 443);

        var values = await consumer.Resource.GetEnvironmentVariableValuesAsync();

        Assert.Equal("https://test123.devtunnels.ms:443", values["services__target__https__0"]);
        Assert.Equal("https://test123.devtunnels.ms:443", values["TARGET_HTTPS"]);
    }

    [Fact]
    public void AddDevTunnel_WithAnonymousAccess_SetsAllowAnonymousOption()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var tunnel = builder.AddDevTunnel("tunnel")
            .WithAnonymousAccess();

        Assert.True(tunnel.Resource.Options.AllowAnonymous);
    }

    [Fact]
    public void AddDevTunnel_WithSpecificTunnelId_SetsTunnelIdProperty()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var tunnel = builder.AddDevTunnel("tunnel", "custom-id");

        Assert.Equal("custom-id", tunnel.Resource.TunnelId);
    }

    [Fact]
    public void WithReference_WithAnonymousAccess_SetsPortAllowAnonymousOption()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var target = builder.AddProject<ProjectA>("target")
            .WithHttpsEndpoint();
        var tunnel = builder.AddDevTunnel("tunnel")
            .WithReference(target, allowAnonymous: true);

        Assert.Single(tunnel.Resource.Ports);
        var port = tunnel.Resource.Ports.First();
        Assert.True(port.Options.AllowAnonymous);
    }

    [Fact]
    public void GetEndpoint_WithResourceAndEndpointName_ReturnsTunnelEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var target = builder.AddProject<ProjectA>("target")
            .WithHttpsEndpoint(name: "https");
        var tunnel = builder.AddDevTunnel("tunnel")
            .WithReference(target);

        var tunnelEndpoint = tunnel.GetEndpoint(target.Resource, "https");

        Assert.NotNull(tunnelEndpoint);
        Assert.Equal(target.Resource, tunnelEndpoint.Resource);
        Assert.Equal(DevTunnelPortResource.TunnelEndpointName, tunnelEndpoint.EndpointName);
    }

    [Fact]
    public void GetEndpoint_WithResourceBuilderAndEndpointName_ReturnsTunnelEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var target = builder.AddProject<ProjectA>("target")
            .WithHttpsEndpoint(name: "https");
        var tunnel = builder.AddDevTunnel("tunnel")
            .WithReference(target);

        var tunnelEndpoint = tunnel.GetEndpoint(target, "https");

        Assert.NotNull(tunnelEndpoint);
        Assert.Equal(target.Resource, tunnelEndpoint.Resource);
        Assert.Equal(DevTunnelPortResource.TunnelEndpointName, tunnelEndpoint.EndpointName);
    }

    [Fact]
    public void GetEndpoint_WithEndpointReference_ReturnsTunnelEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var target = builder.AddProject<ProjectA>("target")
            .WithHttpsEndpoint(name: "https");
        var tunnel = builder.AddDevTunnel("tunnel")
            .WithReference(target);

        var targetEndpoint = target.GetEndpoint("https");
        var tunnelEndpoint = tunnel.GetEndpoint(targetEndpoint);

        Assert.NotNull(tunnelEndpoint);
        Assert.Equal(target.Resource, tunnelEndpoint.Resource);
        Assert.Equal(DevTunnelPortResource.TunnelEndpointName, tunnelEndpoint.EndpointName);
    }

    [Fact]
    public void GetEndpoint_WithResourceAndEndpointName_ReturnsEndpointWithErrorWhenEndpointNotFound()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var target = builder.AddProject<ProjectA>("target")
            .WithHttpsEndpoint(name: "https");
        var tunnel = builder.AddDevTunnel("tunnel")
            .WithReference(target);

        var endpointRef = tunnel.GetEndpoint(target.Resource, "nonexistent");
        
        Assert.NotNull(endpointRef);
        Assert.False(endpointRef.Exists);
        
        var ex = Assert.Throws<InvalidOperationException>(() => _ = endpointRef.EndpointAnnotation);
        Assert.Equal("The dev tunnel 'tunnel' has not been associated with 'nonexistent' on resource 'target'. Use 'WithReference(target)' on the dev tunnel to expose this endpoint.", ex.Message);
    }

    [Fact]
    public void GetEndpoint_WithEndpointReference_ReturnsEndpointWithErrorWhenEndpointNotFound()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var target = builder.AddProject<ProjectA>("target")
            .WithHttpsEndpoint(name: "https");
        var target2 = builder.AddProject<ProjectA>("target2")
            .WithHttpsEndpoint(name: "https");
        var tunnel = builder.AddDevTunnel("tunnel")
            .WithReference(target);

        var target2Endpoint = target2.GetEndpoint("https");
        var endpointRef = tunnel.GetEndpoint(target2Endpoint);
        
        Assert.NotNull(endpointRef);
        Assert.False(endpointRef.Exists);
        
        var ex = Assert.Throws<InvalidOperationException>(() => _ = endpointRef.EndpointAnnotation);
        Assert.Equal("The dev tunnel 'tunnel' has not been associated with 'https' on resource 'target2'. Use 'WithReference(target2)' on the dev tunnel to expose this endpoint.", ex.Message);
    }

    [Fact]
    public void GetEndpoint_WithResourceAndEndpointName_ReturnsEndpointWithErrorWhenResourceNotReferenced()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var target = builder.AddProject<ProjectA>("target")
            .WithHttpsEndpoint(name: "https");
        var tunnel = builder.AddDevTunnel("tunnel");

        var endpointRef = tunnel.GetEndpoint(target.Resource, "https");
        
        Assert.NotNull(endpointRef);
        Assert.False(endpointRef.Exists);
        
        var ex = Assert.Throws<InvalidOperationException>(() => _ = endpointRef.EndpointAnnotation);
        Assert.Equal("The dev tunnel 'tunnel' has not been associated with 'https' on resource 'target'. Use 'WithReference(target)' on the dev tunnel to expose this endpoint.", ex.Message);
    }

    [Fact]
    public void GetEndpoint_WithMultipleEndpoints_ReturnsCorrectTunnelEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var target = builder.AddProject<ProjectA>("target")
            .WithHttpEndpoint(name: "http")
            .WithHttpsEndpoint(name: "https");
        var tunnel = builder.AddDevTunnel("tunnel")
            .WithReference(target);

        var httpTunnelEndpoint = tunnel.GetEndpoint(target.Resource, "http");
        var httpsTunnelEndpoint = tunnel.GetEndpoint(target.Resource, "https");

        Assert.NotNull(httpTunnelEndpoint);
        Assert.NotNull(httpsTunnelEndpoint);
        Assert.Equal(DevTunnelPortResource.TunnelEndpointName, httpTunnelEndpoint.EndpointName);
        Assert.Equal(DevTunnelPortResource.TunnelEndpointName, httpsTunnelEndpoint.EndpointName);
        
        // Verify they reference different ports (implicitly through the annotation)
        Assert.NotSame(httpTunnelEndpoint.EndpointAnnotation, httpsTunnelEndpoint.EndpointAnnotation);
    }

    private sealed class ProjectA : IProjectMetadata
    {
        public string ProjectPath => "projectA";

        public LaunchSettings LaunchSettings { get; } = new();
    }

    private sealed class TestResource(string name) : Resource(name), IResourceWithEnvironment
    {

    }
}

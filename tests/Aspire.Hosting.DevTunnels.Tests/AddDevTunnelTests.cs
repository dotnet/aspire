// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.DevTunnels.Tests;

public class AddDevTunnelTests
{
    [Fact]
    public void AddDevTunnelCreatesResourceWithCorrectType()
    {
        var builder = DistributedApplication.CreateBuilder();
        var tunnel = builder.AddDevTunnel("test-tunnel");
        
        Assert.NotNull(tunnel.Resource);
        Assert.Equal("test-tunnel", tunnel.Resource.Name);
        Assert.IsType<DevTunnelResource>(tunnel.Resource);
    }

    [Fact]
    public void AddDevTunnelWithOptionsConfiguresResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var tunnel = builder.AddDevTunnel("test-tunnel", options =>
        {
            options.AccessToken = "test-token";
            options.Properties["test"] = "value";
        });

        Assert.NotNull(tunnel.Resource.Options);
        Assert.Equal("test-token", tunnel.Resource.Options.AccessToken);
        Assert.Equal("value", tunnel.Resource.Options.Properties["test"]);
    }

    [Fact]
    public void AddDevTunnelWithReferenceCreatesPortResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var webapp = builder.AddProject<TestProject>("webapp")
            .WithHttpEndpoint(port: 8080, name: "http");
        _ = builder.AddDevTunnel("test-tunnel")
            .WithReference(webapp.Resource);

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Should have a tunnel and port resources (http and https by convention)
        var tunnelResource = appModel.Resources.OfType<DevTunnelResource>().Single();
        var portResources = appModel.Resources.OfType<DevTunnelPortResource>().ToList();

        Assert.Equal("test-tunnel", tunnelResource.Name);
        Assert.Equal(2, portResources.Count); // Both http and https
        Assert.All(portResources, p => Assert.Equal(tunnelResource, p.Tunnel));
        
        var httpPort = portResources.Single(p => p.SourceEndpointName == "http");
        Assert.Equal("http", httpPort.Options.Protocol);
    }

    [Fact]
    public void AddDevTunnelWithHttpsReferenceUsesHttpsProtocol()
    {
        var builder = DistributedApplication.CreateBuilder();
        var webapp = builder.AddProject<TestProject>("webapp")
            .WithHttpsEndpoint(port: 8443, name: "https");
        _ = builder.AddDevTunnel("test-tunnel")
            .WithReference(webapp.Resource);

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var portResources = appModel.Resources.OfType<DevTunnelPortResource>().ToList();
        var httpsPort = portResources.Single(p => p.SourceEndpointName == "https");
        Assert.Equal("https", httpsPort.Options.Protocol);
    }

    [Fact]
    public void DevTunnelPortResourceHasParentRelationship()
    {
        var builder = DistributedApplication.CreateBuilder();
        var webapp = builder.AddProject<TestProject>("webapp")
            .WithHttpEndpoint(port: 8080, name: "http");
        _ = builder.AddDevTunnel("test-tunnel")
            .WithReference(webapp.Resource);

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var tunnelResource = appModel.Resources.OfType<DevTunnelResource>().Single();
        var portResources = appModel.Resources.OfType<DevTunnelPortResource>().ToList();

        Assert.Equal(2, portResources.Count); // http and https by convention
        
        // Check parent-child relationship via annotation for each port
        Assert.All(portResources, portResource =>
        {
            var relationship = portResource.Annotations
                .OfType<ResourceRelationshipAnnotation>()
                .SingleOrDefault(r => r.Type == "Parent");

            Assert.NotNull(relationship);
            Assert.Equal(tunnelResource, relationship.Resource);
        });
    }

    [Fact]
    public void DevTunnelPortResourceHasPublicEndpoint()
    {
        var builder = DistributedApplication.CreateBuilder();
        var webapp = builder.AddProject<TestProject>("webapp")
            .WithHttpEndpoint(port: 8080, name: "http");
        _ = builder.AddDevTunnel("test-tunnel")
            .WithReference(webapp.Resource);

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var portResources = appModel.Resources.OfType<DevTunnelPortResource>().ToList();
        Assert.Equal(2, portResources.Count); // http and https by convention

        // Check that each port has public endpoint annotation
        Assert.All(portResources, portResource =>
        {
            var endpointAnnotation = portResource.Annotations
                .OfType<EndpointAnnotation>()
                .SingleOrDefault(e => e.Name == DevTunnelPortResource.PublicEndpointName);

            Assert.NotNull(endpointAnnotation);
            Assert.Equal(portResource.Options.Protocol, endpointAnnotation.UriScheme);
        });
    }

    [Fact]
    public void DevTunnelResourceHasInitialState()
    {
        var builder = DistributedApplication.CreateBuilder();
        var tunnel = builder.AddDevTunnel("test-tunnel");

        var snapshot = tunnel.Resource.Annotations
            .OfType<ResourceSnapshotAnnotation>()
            .Single()
            .InitialSnapshot;

        Assert.Equal("DevTunnel", snapshot.ResourceType);
        Assert.Equal(KnownResourceStates.NotStarted, snapshot.State?.Text);
        var sourceProperty = snapshot.Properties.Single(p => p.Name == CustomResourceKnownProperties.Source);
        Assert.Equal("Aspire.Hosting.DevTunnels", sourceProperty.Value);
    }
}

// Test project placeholder
file sealed class TestProject : IProjectMetadata
{
    public string ProjectPath => "/test/project.csproj";
    public LaunchSettings LaunchSettings => new();
}
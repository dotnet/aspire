// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Immutable;
using Aspire.Dashboard.Model;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class ResourceOutgoingPeerResolverTests
{
    private static ResourceViewModel CreateResource(string name, string? serviceAddress = null, int? servicePort = null)
    {
        ImmutableArray<ResourceServiceViewModel> resourceServices = serviceAddress is null || servicePort is null
            ? ImmutableArray<ResourceServiceViewModel>.Empty
            : [new ResourceServiceViewModel("http", serviceAddress, servicePort)];

        return new ResourceViewModel
        {
            Name = name,
            ResourceType = "Container",
            DisplayName = name,
            Uid = Guid.NewGuid().ToString(),
            CreationTimeStamp = DateTime.UtcNow,
            Environment = ImmutableArray<EnvironmentVariableViewModel>.Empty,
            Endpoints = ImmutableArray<EndpointViewModel>.Empty,
            Services = resourceServices,
            ExpectedEndpointsCount = 0,
            Properties = FrozenDictionary<string, Value>.Empty,
            State = null
        };
    }

    [Fact]
    public void EmptyAttributes_NoMatch()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.False(ResourceOutgoingPeerResolver.TryResolvePeerNameCore(resources, [], out _));
    }

    [Fact]
    public void EmptyUrlAttribute_NoMatch()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.False(ResourceOutgoingPeerResolver.TryResolvePeerNameCore(resources, [KeyValuePair.Create("peer.service", "")], out _));
    }

    [Fact]
    public void NullUrlAttribute_NoMatch()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.False(ResourceOutgoingPeerResolver.TryResolvePeerNameCore(resources, [KeyValuePair.Create<string, string>("peer.service", null!)], out _));
    }

    [Fact]
    public void ExactValueAttribute_Match()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.True(ResourceOutgoingPeerResolver.TryResolvePeerNameCore(resources, [KeyValuePair.Create("peer.service", "localhost:5000")], out var value));
        Assert.Equal("test", value);
    }

    [Fact]
    public void NumberAddressValueAttribute_Match()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.True(ResourceOutgoingPeerResolver.TryResolvePeerNameCore(resources, [KeyValuePair.Create("peer.service", "127.0.0.1:5000")], out var value));
        Assert.Equal("test", value);
    }

    [Fact]
    public void CommaAddressValueAttribute_Match()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.True(ResourceOutgoingPeerResolver.TryResolvePeerNameCore(resources, [KeyValuePair.Create("peer.service", "127.0.0.1,5000")], out var value));
        Assert.Equal("test", value);
    }
}

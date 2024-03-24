// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Immutable;
using Aspire.Dashboard.Model;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class ResourceEndpointHelpersTests
{
    public static List<DisplayedEndpoint> GetEndpoints(ResourceViewModel resource, bool includeInteralUrls = false)
    {
        return ResourceEndpointHelpers.GetEndpoints(resource, includeInteralUrls);
    }

    [Fact]
    public void GetEndpoints_Empty_NoResults()
    {
        var endpoints = GetEndpoints(CreateResource([]));

        Assert.Empty(endpoints);
    }

    [Fact]
    public void GetEndpoints_HasServices_Results()
    {
        var endpoints = GetEndpoints(CreateResource([new("Test", new("http://localhost:8080"), isInternal: false)]));

        Assert.Collection(endpoints,
            e =>
            {
                Assert.Equal("http://localhost:8080", e.Text);
                Assert.Equal("Test", e.Name);
                Assert.Equal("http://localhost:8080", e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8080, e.Port);
            });
    }

    [Fact]
    public void GetEndpoints_HasEndpointAndService_Results()
    {
        var endpoints = GetEndpoints(CreateResource([
            new("Test", new("http://localhost:8080"), isInternal: false),
            new("Test2", new("http://localhost:8081"), isInternal: false)])
        );

        Assert.Collection(endpoints,
            e =>
            {
                Assert.Equal("http://localhost:8080", e.Text);
                Assert.Equal("Test", e.Name);
                Assert.Equal("http://localhost:8080", e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8080, e.Port);
            },
            e =>
            {
                Assert.Equal("http://localhost:8081", e.Text);
                Assert.Equal("Test2", e.Name);
                Assert.Equal("http://localhost:8081", e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8081, e.Port);
            });
    }

    [Fact]
    public void GetEndpoints_OnlyHttpAndHttpsEndpointsSetTheUrl()
    {
        var endpoints = GetEndpoints(CreateResource([
            new("Test", new("http://localhost:8080"), isInternal: false),
            new("Test2", new("tcp://localhost:8081"), isInternal: false)])
        );

        Assert.Collection(endpoints,
            e =>
            {
                Assert.Equal("http://localhost:8080", e.Text);
                Assert.Equal("Test", e.Name);
                Assert.Equal("http://localhost:8080", e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8080, e.Port);
            },
            e =>
            {
                Assert.Equal("tcp://localhost:8081", e.Text);
                Assert.Equal("Test2", e.Name);
                Assert.Null(e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8081, e.Port);
            });
    }

    [Fact]
    public void GetEndpoints_IncludeEndpointUrl_HasEndpointAndService_Results()
    {
        var endpoints = GetEndpoints(CreateResource([
            new("First", new("https://localhost:8080/test"), isInternal:false),
            new("Test", new("https://localhost:8081/test2"), isInternal:false)
        ]));

        Assert.Collection(endpoints,
            e =>
            {
                Assert.Equal("https://localhost:8080/test", e.Text);
                Assert.Equal("First", e.Name);
                Assert.Equal("https://localhost:8080/test", e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8080, e.Port);
            },
            e =>
            {
                Assert.Equal("https://localhost:8081/test2", e.Text);
                Assert.Equal("Test", e.Name);
                Assert.Equal("https://localhost:8081/test2", e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8081, e.Port);
            });
    }

    [Fact]
    public void GetEndpoints_ExlcudesIncludeInternalUrls()
    {
        var endpoints = GetEndpoints(CreateResource([
            new("First", new("https://localhost:8080/test"), isInternal:true),
            new("Test", new("https://localhost:8081/test2"), isInternal:false)
        ]));

        Assert.Collection(endpoints,
            e =>
            {
                Assert.Equal("https://localhost:8081/test2", e.Text);
                Assert.Equal("Test", e.Name);
                Assert.Equal("https://localhost:8081/test2", e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8081, e.Port);
            });
    }

    [Fact]
    public void GetEndpoints_IncludesIncludeInternalUrls()
    {
        var endpoints = GetEndpoints(CreateResource([
            new("First", new("https://localhost:8080/test"), isInternal:true),
            new("Test", new("https://localhost:8081/test2"), isInternal:false)
        ]),
        includeInteralUrls: true);

        Assert.Collection(endpoints,
            e =>
            {
                Assert.Equal("https://localhost:8080/test", e.Text);
                Assert.Equal("First", e.Name);
                Assert.Equal("https://localhost:8080/test", e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8080, e.Port);
            },
            e =>
            {
                Assert.Equal("https://localhost:8081/test2", e.Text);
                Assert.Equal("Test", e.Name);
                Assert.Equal("https://localhost:8081/test2", e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8081, e.Port);
            });
    }

    private static ResourceViewModel CreateResource(ImmutableArray<UrlViewModel> urls)
    {
        return new ResourceViewModel
        {
            Name = "Name!",
            ResourceType = "Container",
            DisplayName = "Display name!",
            Uid = Guid.NewGuid().ToString(),
            CreationTimeStamp = DateTime.UtcNow,
            Environment = [],
            Urls = urls,
            Properties = FrozenDictionary<string, Value>.Empty,
            State = null,
            Commands = []
        };
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Immutable;
using Aspire.Dashboard.Model;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class ResourceEndpointHelpersTests
{
    public static List<DisplayedEndpoint> GetEndpoints(ResourceViewModel resource, bool excludeServices = false, bool includeEndpointUrl = false, ILogger? logger = null)
    {
        return ResourceEndpointHelpers.GetEndpoints(
            logger ?? NullLogger.Instance,
            resource,
            excludeServices: excludeServices,
            includeEndpointUrl: includeEndpointUrl);
    }

    [Fact]
    public void GetEndpoints_Empty_NoResults()
    {
        var endpoints = GetEndpoints(CreateResource(
            endpoints: ImmutableArray<EndpointViewModel>.Empty,
            services: ImmutableArray<ResourceServiceViewModel>.Empty));

        Assert.Empty(endpoints);
    }

    [Fact]
    public void GetEndpoints_HasServices_Results()
    {
        var endpoints = GetEndpoints(CreateResource(
            endpoints: ImmutableArray<EndpointViewModel>.Empty,
            services: [new ResourceServiceViewModel("Test", "localhost", 8080)]));

        Assert.Collection(endpoints,
            e =>
            {
                Assert.Equal("localhost:8080", e.Text);
                Assert.Equal("Test", e.Name);
                Assert.Null(e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8080, e.Port);
            });
    }

    [Fact]
    public void GetEndpoints_HasEndpointAndService_Results()
    {
        var endpoints = GetEndpoints(CreateResource(
            endpoints: [new EndpointViewModel("http://localhost:8080", "http://localhost:8081")],
            services: [new ResourceServiceViewModel("Test", "localhost", 8080), new ResourceServiceViewModel("Test2", "localhost", 8083)]));

        Assert.Collection(endpoints,
            e =>
            {
                Assert.Equal("http://localhost:8081", e.Text);
                Assert.Equal("ProxyUrl", e.Name);
                Assert.Equal("http://localhost:8081", e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8081, e.Port);
            },
            e =>
            {
                Assert.Equal("localhost:8080", e.Text);
                Assert.Equal("Test", e.Name);
                Assert.Null(e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8080, e.Port);
            },
            e =>
            {
                Assert.Equal("localhost:8083", e.Text);
                Assert.Equal("Test2", e.Name);
                Assert.Null(e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8083, e.Port);
            });
    }

    [Fact]
    public void GetEndpoints_HasEndpointAndService_InvalidEndpointUrl_Results()
    {
        var testSink = new TestSink();
        var testLogger = new TestLogger("Test", testSink, enabled: true);

        var endpoints = GetEndpoints(CreateResource(
            endpoints: [new EndpointViewModel("INVALID_URL!@32:TEST", "http://localhost:8081")],
            services: [new ResourceServiceViewModel("Test", "localhost", 8080)]), includeEndpointUrl: true, logger: testLogger);

        Assert.Collection(endpoints,
            e =>
            {
                Assert.Equal("http://localhost:8081", e.Text);
                Assert.Equal("ProxyUrl", e.Name);
                Assert.Equal("http://localhost:8081", e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8081, e.Port);
            },
            e =>
            {
                Assert.Equal("localhost:8080", e.Text);
                Assert.Equal("Test", e.Name);
                Assert.Null(e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8080, e.Port);
            });

        Assert.Collection(testSink.Writes,
            w =>
            {
                Assert.Equal(LogLevel.Warning, w.LogLevel);
                Assert.Equal("Couldn't parse 'INVALID_URL!@32:TEST' to a URI for resource Name!.", w.Message);
            });
    }

    [Fact]
    public void GetEndpoints_IncludeEndpointUrl_HasEndpointAndService_Results()
    {
        var endpoints = GetEndpoints(CreateResource(
            endpoints: [
                    new EndpointViewModel("https://localhost:8080/test", "https://localhost:8081/test2")
                ],
            services: [
                    new ResourceServiceViewModel("First", "localhost", 80),
                    new ResourceServiceViewModel("Test", "localhost", 8080),
                    new ResourceServiceViewModel("Test2", "localhost", 8083)
                ]),
            includeEndpointUrl: true);

        Assert.Collection(endpoints,
            e =>
            {
                Assert.Equal("https://localhost:8080/test", e.Text);
                Assert.Equal("Test", e.Name);
                Assert.Equal("https://localhost:8080/test", e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8080, e.Port);
            },
            e =>
            {
                Assert.Equal("https://localhost:8081/test2", e.Text);
                Assert.Equal("ProxyUrl", e.Name);
                Assert.Equal("https://localhost:8081/test2", e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8081, e.Port);
            },
            e =>
            {
                Assert.Equal("localhost:80", e.Text);
                Assert.Equal("First", e.Name);
                Assert.Null(e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(80, e.Port);
            },
            e =>
            {
                Assert.Equal("localhost:8083", e.Text);
                Assert.Equal("Test2", e.Name);
                Assert.Null(e.Url);
                Assert.Equal("localhost", e.Address);
                Assert.Equal(8083, e.Port);
            });
    }

    private static ResourceViewModel CreateResource(ImmutableArray<EndpointViewModel> endpoints, ImmutableArray<ResourceServiceViewModel> services)
    {
        return new ResourceViewModel
        {
            Name = "Name!",
            ResourceType = "Container",
            DisplayName = "Display name!",
            Uid = Guid.NewGuid().ToString(),
            CreationTimeStamp = DateTime.UtcNow,
            Environment = ImmutableArray<EnvironmentVariableViewModel>.Empty,
            Endpoints = endpoints,
            Services = services,
            ExpectedEndpointsCount = 0,
            Properties = FrozenDictionary<string, Value>.Empty,
            State = null
        };
    }
}

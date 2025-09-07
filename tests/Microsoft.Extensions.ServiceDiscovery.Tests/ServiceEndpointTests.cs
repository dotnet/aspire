// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Xunit;

namespace Microsoft.Extensions.ServiceDiscovery.Tests;

public class ServiceEndpointTests
{
    public static TheoryData<EndPoint> ZeroPortEndPoints => new()
    {
        (EndPoint)IPEndPoint.Parse("127.0.0.1:0"),
        (EndPoint)new DnsEndPoint("microsoft.com", 0),
        (EndPoint)new UriEndPoint(new Uri("https://microsoft.com"))
    };

    public static TheoryData<EndPoint> NonZeroPortEndPoints => new()
    {
        (EndPoint)IPEndPoint.Parse("127.0.0.1:8443"),
        (EndPoint)new DnsEndPoint("microsoft.com", 8443),
        (EndPoint)new UriEndPoint(new Uri("https://microsoft.com:8443"))
    };

    [Theory]
    [MemberData(nameof(ZeroPortEndPoints))]
    public void ServiceEndpointToStringOmitsUnspecifiedPort(EndPoint endpoint)
    {
        var serviceEndpoint = ServiceEndpoint.Create(endpoint);
        var epString = serviceEndpoint.ToString();
        Assert.DoesNotContain(":0", epString);
    }

    [Theory]
    [MemberData(nameof(NonZeroPortEndPoints))]
    public void ServiceEndpointToStringContainsSpecifiedPort(EndPoint endpoint)
    {
        var serviceEndpoint = ServiceEndpoint.Create(endpoint);
        var epString = serviceEndpoint.ToString();
        Assert.Contains(":8443", epString);
    }
}

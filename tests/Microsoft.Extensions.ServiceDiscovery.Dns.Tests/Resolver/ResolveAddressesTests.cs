// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver.Tests;

public class ResolveAddressesTests : LoopbackDnsTestBase
{
    public ResolveAddressesTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResolveIPv4_NoData_Success(bool includeSoa)
    {
        Resolver.Timeout = TimeSpan.FromSeconds(1);

        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            if (includeSoa)
            {
                builder.Authorities.AddStartOfAuthority("ns.com", 240, "ns.com", "admin.ns.com", 1, 900, 180, 6048000, 3600);
            }
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetwork);
        Assert.Empty(results);

        if (includeSoa)
        {
            // if SOA is included, the negative result can be cached
            Assert.Empty(await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetwork));

            // negative result does not affect other types
            await Assert.ThrowsAsync<TimeoutException>(async () => await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetworkV6));
        }
        else
        {
            // no caching -> new request, and the request should timeout
            await Assert.ThrowsAsync<TimeoutException>(async () => await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetwork));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResolveIPv4_NoSuchName_Success(bool includeSoa)
    {
        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.ResponseCode = QueryResponseCode.NameError;
            if (includeSoa)
            {
                builder.Authorities.AddStartOfAuthority("ns.com", 240, "ns.com", "admin.ns.com", 1, 900, 180, 6048000, 3600);
            }
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetwork);
        Assert.Empty(results);

        if (includeSoa)
        {
            // if SOA is included, the negative result can be cached for all types
            Assert.Empty(await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetwork));
            Assert.Empty(await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetworkV6));
        }
        else
        {
            Resolver.Timeout = TimeSpan.FromSeconds(1);
            // no caching -> new request, and the request should timeout
            await Assert.ThrowsAsync<TimeoutException>(async () => await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetwork));
        }
    }

    [Fact]
    public async Task ResolveIPv4_Simple_Success()
    {
        IPAddress address = IPAddress.Parse("172.213.245.111");
        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Answers.AddAddress("www.example.com", 3600, address);
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetwork);

        AddressResult res = Assert.Single(results);
        Assert.Equal(address, res.Address);
        Assert.Equal(TimeProvider.GetUtcNow().DateTime.AddSeconds(3600), res.ExpiresAt);
    }

    [Fact]
    public async Task ResolveIPv4_Aliases_Success()
    {
        IPAddress address = IPAddress.Parse("172.213.245.111");
        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Answers.AddCname("www.example.com", 3600, "www.example2.com");
            builder.Answers.AddCname("www.example2.com", 3600, "www.example3.com");
            builder.Answers.AddAddress("www.example3.com", 3600, address);
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetwork);

        AddressResult res = Assert.Single(results);
        Assert.Equal(address, res.Address);
        Assert.Equal(TimeProvider.GetUtcNow().DateTime.AddSeconds(3600), res.ExpiresAt);
    }

    [Fact]
    public async Task ResolveIPv4_Aliases_NotFound_Success()
    {
        IPAddress address = IPAddress.Parse("172.213.245.111");
        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Answers.AddCname("www.example.com", 3600, "www.example2.com");
            builder.Answers.AddCname("www.example2.com", 3600, "www.example3.com");

            // extra address in the answer not connected to the above
            builder.Answers.AddAddress("www.example4.com", 3600, address);
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetwork);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ResolveIP_InvalidAddressFamily_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.Unknown));
    }

    [Fact]
    public async Task ResolveIP_Cached_Success()
    {
        IPAddress address = IPAddress.Parse("172.213.245.111");
        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Answers.AddAddress("www.example.com", 3600, address);
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetwork);

        AddressResult res = Assert.Single(results);
        DnsServer.Dispose();

        AddressResult cached = Assert.Single(await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetwork));
        Assert.Equal(res, cached);
    }
}

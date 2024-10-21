// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver.Tests;

public class TcpFailoverTests : LoopbackDnsTestBase
{
    public TcpFailoverTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task TcpFailover_Simple_Success()
    {
        IPAddress address = IPAddress.Parse("172.213.245.111");
        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Flags |= QueryFlags.ResultTruncated;
            return Task.CompletedTask;
        });

        _ = DnsServer.ProcessTcpRequest(builder =>
        {
            builder.Answers.AddAddress("www.example.com", 3600, address);
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetwork);

        AddressResult res = Assert.Single(results);
        Assert.Equal(address, res.Address);
        Assert.Equal(TimeProvider.GetUtcNow().DateTime.AddSeconds(3600), res.ExpiresAt);
    }
}

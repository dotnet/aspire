// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver.Tests;

public class RetryTests : LoopbackDnsTestBase
{
    public RetryTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Retry_Simple_Success()
    {
        Options.Attempts = 3;
        IPAddress address = IPAddress.Parse("172.213.245.111");

        _ = Task.Run(async () =>
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                await DnsServer.ProcessUdpRequest(builder =>
                {
                    if (attempt == 3)
                    {
                        builder.Answers.AddAddress("www.example.com", 3600, address);
                    }
                    else
                    {
                        builder.ResponseCode = QueryResponseCode.ServerFailure;
                    }
                    return Task.CompletedTask;
                });
            }
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetwork);

        AddressResult res = Assert.Single(results);
        Assert.Equal(address, res.Address);
        Assert.Equal(TimeProvider.GetUtcNow().DateTime.AddSeconds(3600), res.ExpiresAt);

    }
}

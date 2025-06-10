// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Net.Sockets;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver.Tests;

public class CancellationTests : LoopbackDnsTestBase
{
    public CancellationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task PreCanceledToken_Throws()
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await Resolver.ResolveIPAddressesAsync("example.com", AddressFamily.InterNetwork, cts.Token));

        Assert.Equal(cts.Token, ex.CancellationToken);
    }

    [Fact]
    public async Task CancellationInProgress_Throws()
    {
        CancellationTokenSource cts = new CancellationTokenSource();

        var task = Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await Resolver.ResolveIPAddressesAsync("example.com", AddressFamily.InterNetwork, cts.Token));

        await DnsServer.ProcessUdpRequest(_ =>
        {
            cts.Cancel();
            return Task.CompletedTask;
        });

        OperationCanceledException ex = await task;
        Assert.Equal(cts.Token, ex.CancellationToken);
    }
}

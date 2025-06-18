// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
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
        string hostName = "tcp-simple.test";
        IPAddress address = IPAddress.Parse("172.213.245.111");

        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Flags |= QueryFlags.ResultTruncated;
            return Task.CompletedTask;
        });

        _ = DnsServer.ProcessTcpRequest(builder =>
        {
            builder.Answers.AddAddress(hostName, 3600, address);
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync(hostName, AddressFamily.InterNetwork);

        AddressResult res = Assert.Single(results);
        Assert.Equal(address, res.Address);
        Assert.Equal(TimeProvider.GetUtcNow().DateTime.AddSeconds(3600), res.ExpiresAt);
    }

    [Fact]
    public async Task TcpFailover_ServerClosesWithoutData_EmptyResult()
    {
        string hostName = "tcp-server-closes.test";
        Options.Attempts = 1;
        Options.Timeout = TimeSpan.FromSeconds(60);

        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Flags |= QueryFlags.ResultTruncated;
            return Task.CompletedTask;
        });

        Task serverTask = DnsServer.ProcessTcpRequest(builder =>
        {
            throw new InvalidOperationException("This forces closing the socket without writing any data");
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync(hostName, AddressFamily.InterNetwork).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Empty(results);

        await Assert.ThrowsAsync<InvalidOperationException>(() => serverTask);
    }

    [Fact]
    public async Task TcpFailover_TcpNotAvailable_EmptyResult()
    {
        string hostName = "tcp-not-available.test";
        Options.Attempts = 1;
        Options.Timeout = TimeSpan.FromMilliseconds(100000);

        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Flags |= QueryFlags.ResultTruncated;
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync(hostName, AddressFamily.InterNetwork);
        Assert.Empty(results);
    }

    [Fact]
    public async Task TcpFailover_HeaderMismatch_ReturnsEmpty()
    {
        string hostName = "tcp-header-mismatch.test";
        Options.Timeout = TimeSpan.FromSeconds(1);
        IPAddress address = IPAddress.Parse("172.213.245.111");

        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Flags |= QueryFlags.ResultTruncated;
            return Task.CompletedTask;
        });

        _ = DnsServer.ProcessTcpRequest(builder =>
        {
            builder.TransactionId++;
            builder.Answers.AddAddress(hostName, 3600, address);
            return Task.CompletedTask;
        });

        AddressResult[] result = await Resolver.ResolveIPAddressesAsync(hostName, AddressFamily.InterNetwork);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("not-example.com", (int)QueryType.A, (int)QueryClass.Internet)]
    [InlineData("example.com", (int)QueryType.AAAA, (int)QueryClass.Internet)]
    [InlineData("example.com", (int)QueryType.A, 0)]
    public async Task TcpFailover_QuestionMismatch_ReturnsEmpty(string name, int type, int @class)
    {
        string hostName = "tcp-question-mismatch.test";
        Options.Timeout = TimeSpan.FromSeconds(1);
        IPAddress address = IPAddress.Parse("172.213.245.111");

        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Flags |= QueryFlags.ResultTruncated;
            return Task.CompletedTask;
        });

        _ = DnsServer.ProcessTcpRequest(builder =>
        {
            builder.Questions[0] = (name, (QueryType)type, (QueryClass)@class);
            builder.Answers.AddAddress(hostName, 3600, address);
            return Task.CompletedTask;
        });

        AddressResult[] result = await Resolver.ResolveIPAddressesAsync(hostName, AddressFamily.InterNetwork);
        Assert.Empty(result);
    }
}

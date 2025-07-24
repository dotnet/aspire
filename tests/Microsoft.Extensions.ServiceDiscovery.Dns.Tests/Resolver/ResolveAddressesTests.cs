// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
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
        string hostName = "nodata.test";

        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            if (includeSoa)
            {
                builder.Authorities.AddStartOfAuthority("ns.com", 240, "ns.com", "admin.ns.com", 1, 900, 180, 6048000, 3600);
            }
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync(hostName, AddressFamily.InterNetwork);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResolveIPv4_NoSuchName_Success(bool includeSoa)
    {
        string hostName = "nosuchname.test";

        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.ResponseCode = QueryResponseCode.NameError;
            if (includeSoa)
            {
                builder.Authorities.AddStartOfAuthority("ns.com", 240, "ns.com", "admin.ns.com", 1, 900, 180, 6048000, 3600);
            }
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync(hostName, AddressFamily.InterNetwork);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("www.resolveipv4.com")]
    [InlineData("www.resolveipv4.com.")]
    [InlineData("www.ř.com")]
    public async Task ResolveIPv4_Simple_Success(string name)
    {
        IPAddress address = IPAddress.Parse("172.213.245.111");
        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Answers.AddAddress(name, 3600, address);
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync(name, AddressFamily.InterNetwork);

        AddressResult res = Assert.Single(results);
        Assert.Equal(address, res.Address);
        Assert.Equal(TimeProvider.GetUtcNow().DateTime.AddSeconds(3600), res.ExpiresAt);
    }

    [Fact]
    public async Task ResolveIPv4_Aliases_InOrder_Success()
    {
        IPAddress address = IPAddress.Parse("172.213.245.111");
        string hostName = "alias-in-order.test";

        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Answers.AddCname(hostName, 3600, "www.example2.com");
            builder.Answers.AddCname("www.example2.com", 3600, "www.example3.com");
            builder.Answers.AddAddress("www.example3.com", 3600, address);
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync(hostName, AddressFamily.InterNetwork);

        AddressResult res = Assert.Single(results);
        Assert.Equal(address, res.Address);
        Assert.Equal(TimeProvider.GetUtcNow().DateTime.AddSeconds(3600), res.ExpiresAt);
    }

    [Fact]
    public async Task ResolveIPv4_Aliases_OutOfOrder_Success()
    {
        IPAddress address = IPAddress.Parse("172.213.245.111");
        string hostName = "alias-out-of-order.test";

        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Answers.AddCname("www.example2.com", 3600, "www.example3.com");
            builder.Answers.AddAddress("www.example3.com", 3600, address);
            builder.Answers.AddCname(hostName, 3600, "www.example2.com");
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync(hostName, AddressFamily.InterNetwork);

        AddressResult res = Assert.Single(results);
        Assert.Equal(address, res.Address);
        Assert.Equal(TimeProvider.GetUtcNow().DateTime.AddSeconds(3600), res.ExpiresAt);
    }

    [Fact]
    public async Task ResolveIPv4_Aliases_Loop_ReturnsEmpty()
    {
        string hostName = "alias-loop2.test";

        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Answers.AddCname(hostName, 3600, "www.example2.com");
            builder.Answers.AddCname("www.example2.com", 3600, "www.example3.com");
            builder.Answers.AddCname("www.example3.com", 3600, hostName);
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync(hostName, AddressFamily.InterNetwork);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ResolveIPv4_Aliases_Loop_Reverse_ReturnsEmpty()
    {
        string hostName = "alias-loop2.test";

        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Answers.AddCname("www.example3.com", 3600, hostName);
            builder.Answers.AddCname("www.example2.com", 3600, "www.example3.com");
            builder.Answers.AddCname(hostName, 3600, "www.example2.com");
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync(hostName, AddressFamily.InterNetwork);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ResolveIPv4_Alias_And_Address()
    {
        IPAddress address = IPAddress.Parse("172.213.245.111");
        string hostName = "alias-address.test";

        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Answers.AddCname(hostName, 3600, "www.example2.com");
            builder.Answers.AddCname("www.example2.com", 3600, "www.example3.com");
            builder.Answers.AddAddress("www.example2.com", 3600, address);
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync(hostName, AddressFamily.InterNetwork);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ResolveIPv4_DuplicateAlias()
    {
        IPAddress address = IPAddress.Parse("172.213.245.111");
        string hostName = "duplicate-alias.test";

        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Answers.AddCname(hostName, 3600, "www.example2.com");
            builder.Answers.AddCname("www.example2.com", 3600, "www.example3.com");
            builder.Answers.AddCname("www.example2.com", 3600, "www.example4.com");
            builder.Answers.AddAddress("www.example2.com", 3600, address);
            builder.Answers.AddAddress("www.example4.com", 3600, address);
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync(hostName, AddressFamily.InterNetwork);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ResolveIPv4_Aliases_NotFound_Success()
    {
        IPAddress address = IPAddress.Parse("172.213.245.111");
        string hostName = "alias-no-found.test";

        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Answers.AddCname(hostName, 3600, "www.example2.com");
            builder.Answers.AddCname("www.example2.com", 3600, "www.example3.com");

            // extra address in the answer not connected to the above
            builder.Answers.AddAddress("www.example4.com", 3600, address);
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync(hostName, AddressFamily.InterNetwork);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ResolveIP_InvalidAddressFamily_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await Resolver.ResolveIPAddressesAsync("invalid-af.test", AddressFamily.Unknown));
    }

    [Theory]
    [InlineData(AddressFamily.InterNetwork, "127.0.0.1")]
    [InlineData(AddressFamily.InterNetworkV6, "::1")]
    public async Task ResolveIP_Localhost_ReturnsLoopback(AddressFamily family, string addressAsString)
    {
        IPAddress address = IPAddress.Parse(addressAsString);
        AddressResult[] results = await Resolver.ResolveIPAddressesAsync("localhost", family);
        AddressResult result = Assert.Single(results);

        Assert.Equal(address, result.Address);
    }

    [Fact]
    public async Task Resolve_Timeout_ReturnsEmpty()
    {
        Options.Timeout = TimeSpan.FromSeconds(1);
        AddressResult[] result = await Resolver.ResolveIPAddressesAsync("timeout-empty.test", AddressFamily.InterNetwork);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("not-example.com", (int)QueryType.A, (int)QueryClass.Internet)]
    [InlineData("example.com", (int)QueryType.AAAA, (int)QueryClass.Internet)]
    [InlineData("example.com", (int)QueryType.A, 0)]
    public async Task Resolve_QuestionMismatch_ReturnsEmpty(string name, int type, int @class)
    {
        Options.Timeout = TimeSpan.FromSeconds(1);

        IPAddress address = IPAddress.Parse("172.213.245.111");
        _ = DnsServer.ProcessUdpRequest(builder =>
        {
            builder.Questions[0] = (name, (QueryType)type, (QueryClass)@class);
            builder.Answers.AddAddress("www.example4.com", 3600, address);
            return Task.CompletedTask;
        });

        AddressResult[] result = await Resolver.ResolveIPAddressesAsync("example.com", AddressFamily.InterNetwork);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Resolve_HeaderMismatch_Ignores()
    {
        string name = "header-mismatch.test";
        Options.Timeout = TimeSpan.FromSeconds(5);

        SemaphoreSlim responseSemaphore = new SemaphoreSlim(0, 1);
        SemaphoreSlim requestSemaphore = new SemaphoreSlim(0, 1);

        IPEndPoint clientAddress = null!;

        IPAddress address = IPAddress.Parse("172.213.245.111");
        ushort transactionId = 0x1234;
        _ = DnsServer.ProcessUdpRequest((builder, clientAddr) =>
        {
            clientAddress = clientAddr;
            transactionId = (ushort)(builder.TransactionId + 1);

            builder.Answers.AddAddress(name, 3600, address);
            requestSemaphore.Release();
            return responseSemaphore.WaitAsync();
        });

        ValueTask<AddressResult[]> task = Resolver.ResolveIPAddressesAsync(name, AddressFamily.InterNetwork);

        await requestSemaphore.WaitAsync().WaitAsync(Options.Timeout);

        using Socket socket = new Socket(clientAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        LoopbackDnsResponseBuilder responseBuilder = new LoopbackDnsResponseBuilder(name, QueryType.A, QueryClass.Internet)
        {
            TransactionId = transactionId,
            ResponseCode = QueryResponseCode.NoError
        };

        responseBuilder.Questions.Add((name, QueryType.A, QueryClass.Internet));
        responseBuilder.Answers.AddAddress(name, 3600, IPAddress.Loopback);
        socket.SendTo(responseBuilder.GetMessageBytes(), clientAddress);

        responseSemaphore.Release();

        AddressResult[] results = await task;
        AddressResult result = Assert.Single(results);

        Assert.Equal(address, result.Address);
    }
}
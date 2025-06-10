// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using Xunit;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver.Tests;

public class RetryTests : LoopbackDnsTestBase
{
    public RetryTests(ITestOutputHelper output) : base(output)
    {
        Options.Attempts = 3;
    }

    private static void SetupUdpProcessFunction(LoopbackDnsServer server, Func<LoopbackDnsResponseBuilder, Task> func)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    await server.ProcessUdpRequest(func);
                }
            }
            catch (SocketException)
            {
                // Test teardown closed the socket, ignore
            }
        });
    }

    private void SetupUdpProcessFunction(Func<LoopbackDnsResponseBuilder, Task> func)
    {
        SetupUdpProcessFunction(DnsServer, func);
    }

    [Fact]
    public async Task Retry_Simple_Success()
    {
        IPAddress address = IPAddress.Parse("172.213.245.111");

        int attempt = 0;

        SetupUdpProcessFunction(builder =>
        {
            attempt++;
            if (attempt == Options.Attempts)
            {
                builder.Answers.AddAddress("www.example.com", 3600, address);
            }
            else
            {
                builder.ResponseCode = QueryResponseCode.ServerFailure;
            }
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetwork);

        AddressResult res = Assert.Single(results);
        Assert.Equal(address, res.Address);
        Assert.Equal(TimeProvider.GetUtcNow().DateTime.AddSeconds(3600), res.ExpiresAt);
    }

    public enum PersistentErrorType
    {
        NotImplemented,
        Refused,
        MalformedResponse
    }

    [Theory]
    [InlineData(PersistentErrorType.NotImplemented)]
    [InlineData(PersistentErrorType.Refused)]
    [InlineData(PersistentErrorType.MalformedResponse)]
    public async Task PersistentErrorsResponseCode_FailoverToNextServer(PersistentErrorType type)
    {
        IPAddress address = IPAddress.Parse("172.213.245.111");

        int primaryAttempt = 0;
        int secondaryAttempt = 0;

        AddressResult[] results = await RunWithFallbackServerHelper("www.example.com",
            builder =>
            {
                primaryAttempt++;
                switch (type)
                {
                    case PersistentErrorType.NotImplemented:
                        builder.ResponseCode = QueryResponseCode.NotImplemented;
                        break;

                    case PersistentErrorType.Refused:
                        builder.ResponseCode = QueryResponseCode.Refused;
                        break;

                    case PersistentErrorType.MalformedResponse:
                        builder.ResponseCode = (QueryResponseCode)0xFF;
                        break;
                }
                return Task.CompletedTask;
            },
            builder =>
            {
                secondaryAttempt++;
                builder.Answers.AddAddress("www.example.com", 3600, address);
                return Task.CompletedTask;
            });

        Assert.Equal(1, primaryAttempt);
        Assert.Equal(1, secondaryAttempt);

        AddressResult res = Assert.Single(results);
        Assert.Equal(address, res.Address);
        Assert.Equal(TimeProvider.GetUtcNow().DateTime.AddSeconds(3600), res.ExpiresAt);
    }

    public enum DefinitveAnswerType
    {
        NoError,
        NoData,
        NameError,
    }

    [Theory]
    [InlineData(DefinitveAnswerType.NoError, false)]
    [InlineData(DefinitveAnswerType.NoData, false)]
    [InlineData(DefinitveAnswerType.NoData, true)]
    [InlineData(DefinitveAnswerType.NameError, false)]
    [InlineData(DefinitveAnswerType.NameError, true)]
    public async Task DefinitiveAnswers_NoRetryOrFailover(DefinitveAnswerType type, bool additionalData)
    {
        IPAddress address = IPAddress.Parse("172.213.245.111");

        int primaryAttempt = 0;
        int secondaryAttempt = 0;

        AddressResult[] results = await RunWithFallbackServerHelper("www.example.com",
            builder =>
            {
                primaryAttempt++;
                switch (type)
                {
                    case DefinitveAnswerType.NoError:
                        builder.ResponseCode = QueryResponseCode.NoError;
                        builder.Answers.AddAddress("www.example.com", 3600, address);
                        break;

                    case DefinitveAnswerType.NoData:
                        builder.ResponseCode = QueryResponseCode.NoError;
                        break;

                    case DefinitveAnswerType.NameError:
                        builder.ResponseCode = QueryResponseCode.NameError;
                        break;
                }

                if (additionalData)
                {
                    builder.Authorities.AddStartOfAuthority("www.example.com", 300, "ns1.example.com", "hostmaster.example.com", 2023101001, 1, 3600, 300, 86400);
                }

                return Task.CompletedTask;
            },
            builder =>
            {
                secondaryAttempt++;
                builder.ResponseCode = QueryResponseCode.Refused;
                return Task.CompletedTask;
            });

        Assert.Equal(1, primaryAttempt);
        Assert.Equal(0, secondaryAttempt);

        if (type == DefinitveAnswerType.NoError)
        {
            AddressResult res = Assert.Single(results);
            Assert.Equal(address, res.Address);
            Assert.Equal(TimeProvider.GetUtcNow().DateTime.AddSeconds(3600), res.ExpiresAt);
        }
        else
        {
            Assert.Empty(results);
        }
    }

    [Fact]
    public async Task ExhaustedRetries_FailoverToNextServer()
    {
        IPAddress address = IPAddress.Parse("172.213.245.111");

        int primaryAttempt = 0;
        int secondaryAttempt = 0;

        AddressResult[] results = await RunWithFallbackServerHelper("www.example.com",
            builder =>
            {
                primaryAttempt++;
                builder.ResponseCode = QueryResponseCode.ServerFailure;
                return Task.CompletedTask;
            },
            builder =>
            {
                secondaryAttempt++;
                builder.Answers.AddAddress("www.example.com", 3600, address);
                return Task.CompletedTask;
            });

        Assert.Equal(Options.Attempts, primaryAttempt);
        Assert.Equal(1, secondaryAttempt);

        AddressResult res = Assert.Single(results);
        Assert.Equal(address, res.Address);
        Assert.Equal(TimeProvider.GetUtcNow().DateTime.AddSeconds(3600), res.ExpiresAt);
    }

    public enum TransientErrorType
    {
        Timeout,
        ServerFailure,
        // TODO: simulate NetworkErrors
    }

    [Theory]
    [InlineData(TransientErrorType.Timeout)]
    [InlineData(TransientErrorType.ServerFailure)]
    public async Task TransientError_RetryOnSameServer(TransientErrorType type)
    {
        IPAddress address = IPAddress.Parse("172.213.245.111");

        int primaryAttempt = 0;
        int secondaryAttempt = 0;

        AddressResult[] results = await RunWithFallbackServerHelper("www.example.com",
            async builder =>
            {
                primaryAttempt++;
                if (primaryAttempt == 1)
                {
                    switch (type)
                    {
                        case TransientErrorType.Timeout:
                            await Task.Delay(Options.Timeout.Multiply(1.5));
                            builder.Answers.AddAddress("www.example.com", 3600, address);
                            break;

                        case TransientErrorType.ServerFailure:
                            builder.ResponseCode = QueryResponseCode.ServerFailure;
                            break;
                    }
                }
                else
                {
                    builder.Answers.AddAddress("www.example.com", 3600, address);
                }
            },
            builder =>
            {
                secondaryAttempt++;
                builder.ResponseCode = QueryResponseCode.Refused;
                return Task.CompletedTask;
            });

        Assert.Equal(2, primaryAttempt);
        Assert.Equal(0, secondaryAttempt);

        AddressResult res = Assert.Single(results);
        Assert.Equal(address, res.Address);
        Assert.Equal(TimeProvider.GetUtcNow().DateTime.AddSeconds(3600), res.ExpiresAt);
    }

    private async Task<AddressResult[]> RunWithFallbackServerHelper(string name, Func<LoopbackDnsResponseBuilder, Task> primaryHandler, Func<LoopbackDnsResponseBuilder, Task> fallbackHandler)
    {
        SetupUdpProcessFunction(primaryHandler);
        using LoopbackDnsServer fallbackServer = new LoopbackDnsServer();
        SetupUdpProcessFunction(fallbackServer, fallbackHandler);

        Options.Servers = [DnsServer.DnsEndPoint, fallbackServer.DnsEndPoint];

        return await Resolver.ResolveIPAddressesAsync(name, AddressFamily.InterNetwork);
    }

    [Fact]
    public async Task NameError_NoRetry()
    {
        int counter = 0;
        SetupUdpProcessFunction(builder =>
        {
            counter++;
            // authoritative answer that the name does not exist
            builder.ResponseCode = QueryResponseCode.NameError;
            return Task.CompletedTask;
        });

        AddressResult[] results = await Resolver.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetwork);

        Assert.Empty(results);
        Assert.Equal(1, counter);
    }
}

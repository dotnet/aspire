// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Yarp.ReverseProxy.Configuration;
using System.Net;
using DnsClient;
using DnsClient.Protocol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Yarp.Tests;

/// <summary>
/// Tests for YARP with Service Discovery enabled.
/// </summary>
public class YarpServiceDiscoveryTests
{
    private static ServiceDiscoveryDestinationResolver CreateResolver(IServiceProvider serviceProvider)
    {
        var coreResolver = serviceProvider.GetRequiredService<ServiceEndpointResolver>();
        return new ServiceDiscoveryDestinationResolver(
            coreResolver,
            serviceProvider.GetRequiredService<IOptions<ServiceDiscoveryOptions>>());
    }

    [Fact]
    public async Task ServiceDiscoveryDestinationResolverTests_PassThrough()
    {
        await using var services = new ServiceCollection()
            .AddServiceDiscoveryCore()
            .AddPassThroughServiceEndpointProvider()
            .BuildServiceProvider();
        var yarpResolver = CreateResolver(services);

        var destinationConfigs = new Dictionary<string, DestinationConfig>
        {
            ["dest-a"] = new()
            {
                Address = "https://my-svc",
            },
        };

        var result = await yarpResolver.ResolveDestinationsAsync(destinationConfigs, CancellationToken.None);

        Assert.Single(result.Destinations);
        Assert.Collection(result.Destinations.Select(d => d.Value.Address),
            a => Assert.Equal("https://my-svc/", a));
    }

    [Fact]
    public async Task ServiceDiscoveryDestinationResolverTests_Configuration()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["services:basket:default:0"] = "ftp://localhost:2121",
            ["services:basket:default:1"] = "https://localhost:8888",
            ["services:basket:default:2"] = "http://localhost:1111",
        });
        await using var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore()
            .AddConfigurationServiceEndpointProvider()
            .BuildServiceProvider();
        var yarpResolver = CreateResolver(services);

        var destinationConfigs = new Dictionary<string, DestinationConfig>
        {
            ["dest-a"] = new()
            {
                Address = "https+http://basket",
            },
        };

        var result = await yarpResolver.ResolveDestinationsAsync(destinationConfigs, CancellationToken.None);

        Assert.Single(result.Destinations);
        Assert.Collection(result.Destinations.Select(d => d.Value.Address),
            a => Assert.Equal("https://localhost:8888/", a));
    }

    [Fact]
    public async Task ServiceDiscoveryDestinationResolverTests_Configuration_NonPreferredScheme()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["services:basket:default:0"] = "ftp://localhost:2121",
            ["services:basket:default:1"] = "http://localhost:1111",
        });
        await using var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore()
            .AddConfigurationServiceEndpointProvider()
            .BuildServiceProvider();
        var yarpResolver = CreateResolver(services);

        var destinationConfigs = new Dictionary<string, DestinationConfig>
        {
            ["dest-a"] = new()
            {
                Address = "https+http://basket",
            },
        };

        var result = await yarpResolver.ResolveDestinationsAsync(destinationConfigs, CancellationToken.None);

        Assert.Single(result.Destinations);
        Assert.Collection(result.Destinations.Select(d => d.Value.Address),
            a => Assert.Equal("http://localhost:1111/", a));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ServiceDiscoveryDestinationResolverTests_Configuration_Host_Value(bool configHasHost)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["services:basket:default:0"] = "https://localhost:1111",
            ["services:basket:default:1"] = "https://127.0.0.1:2222",
            ["services:basket:default:2"] = "https://[::1]:3333",
            ["services:basket:default:3"] = "https://baskets-galore.faketld",
        });
        await using var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore()
            .AddConfigurationServiceEndpointProvider()
            .BuildServiceProvider();
        var yarpResolver = CreateResolver(services);

        var destinationConfigs = new Dictionary<string, DestinationConfig>
        {
            ["dest-a"] = new()
            {
                Address = "https://basket",
                Host = configHasHost ? "my-basket-svc.faketld" : null
            },
        };

        var result = await yarpResolver.ResolveDestinationsAsync(destinationConfigs, CancellationToken.None);

        Assert.Equal(4, result.Destinations.Count);
        Assert.Collection(result.Destinations.Values,
            a =>
            {
                Assert.Equal("https://localhost:1111/", a.Address);
                if (configHasHost)
                {
                    Assert.Equal("my-basket-svc.faketld", a.Host);
                }
                else
                {
                    Assert.Null(a.Host);
                }
            },
            a =>
            {
                Assert.Equal("https://127.0.0.1:2222/", a.Address);
                if (configHasHost)
                {
                    Assert.Equal("my-basket-svc.faketld", a.Host);
                }
                else
                {
                    Assert.Null(a.Host);
                }
            },
            a =>
            {
                Assert.Equal("https://[::1]:3333/", a.Address);
                if (configHasHost)
                {
                    Assert.Equal("my-basket-svc.faketld", a.Host);
                }
                else
                {
                    Assert.Null(a.Host);
                }
            },
            a =>
            {
                Assert.Equal("https://baskets-galore.faketld/", a.Address);
                if (configHasHost)
                {
                    Assert.Equal("my-basket-svc.faketld", a.Host);
                }
                else
                {
                    Assert.Null(a.Host);
                }
            });
    }

    [Fact]
    public async Task ServiceDiscoveryDestinationResolverTests_Configuration_DisallowedScheme()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["services:basket:default:0"] = "ftp://localhost:2121",
            ["services:basket:default:1"] = "http://localhost:1111",
        });
        await using var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore()
            .Configure<ServiceDiscoveryOptions>(o =>
            {
                // Allow only "https://"
                o.AllowAllSchemes = false;
                o.AllowedSchemes = ["https"];
            })
            .AddConfigurationServiceEndpointProvider()
            .BuildServiceProvider();
        var yarpResolver = CreateResolver(services);

        var destinationConfigs = new Dictionary<string, DestinationConfig>
        {
            ["dest-a"] = new()
            {
                Address = "https+http://basket",
            },
        };

        var result = await yarpResolver.ResolveDestinationsAsync(destinationConfigs, CancellationToken.None);

        // No results: there are no 'https' endpoints in config and 'http' is disallowed.
        Assert.Empty(result.Destinations);
    }

    [Fact]
    public async Task ServiceDiscoveryDestinationResolverTests_Dns()
    {
        await using var services = new ServiceCollection()
            .AddServiceDiscoveryCore()
            .AddDnsServiceEndpointProvider()
            .BuildServiceProvider();
        var yarpResolver = CreateResolver(services);

        var destinationConfigs = new Dictionary<string, DestinationConfig>
        {
            ["dest-a"] = new()
            {
                Address = "https://microsoft.com",
            },
            ["dest-b"] = new()
            {
                Address = "http://msn.com",
            },
        };

        var result = await yarpResolver.ResolveDestinationsAsync(destinationConfigs, CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Destinations);
        Assert.All(result.Destinations, d =>
        {
            var address = d.Value.Address;
            Assert.True(Uri.TryCreate(address, default, out var uri), $"Failed to parse address '{address}' as URI.");
            Assert.True(uri.IsDefaultPort, "URI should use the default port when resolved via DNS.");
            var expectedScheme = d.Key.StartsWith("dest-a") ? "https" : "http";
            Assert.Equal(expectedScheme, uri.Scheme);
        });
    }

    [Fact]
    public async Task ServiceDiscoveryDestinationResolverTests_DnsSrv()
    {
        var dnsClientMock = new FakeDnsClient
        {
            QueryAsyncFunc = (query, queryType, queryClass, cancellationToken) =>
            {
                var response = new FakeDnsQueryResponse
                {
                    Answers = new List<DnsResourceRecord>
                    {
                        new SrvRecord(new ResourceRecordInfo(query, ResourceRecordType.SRV, queryClass, 123, 0), 99, 66, 8888, DnsString.Parse("srv-a")),
                        new SrvRecord(new ResourceRecordInfo(query, ResourceRecordType.SRV, queryClass, 123, 0), 99, 62, 9999, DnsString.Parse("srv-b")),
                        new SrvRecord(new ResourceRecordInfo(query, ResourceRecordType.SRV, queryClass, 123, 0), 99, 62, 7777, DnsString.Parse("srv-c"))
                    },
                    Additionals = new List<DnsResourceRecord>
                    {
                        new ARecord(new ResourceRecordInfo("srv-a", ResourceRecordType.A, queryClass, 64, 0), IPAddress.Parse("10.10.10.10")),
                        new ARecord(new ResourceRecordInfo("srv-b", ResourceRecordType.AAAA, queryClass, 64, 0), IPAddress.IPv6Loopback),
                        new ARecord(new ResourceRecordInfo("srv-c", ResourceRecordType.A, queryClass, 64, 0), IPAddress.Loopback),
                    }
                };

                return Task.FromResult<IDnsQueryResponse>(response);
            }
        };

        await using var services = new ServiceCollection()
            .AddSingleton<IDnsQuery>(dnsClientMock)
            .AddServiceDiscoveryCore()
            .AddDnsSrvServiceEndpointProvider(options => options.QuerySuffix = ".ns")
            .BuildServiceProvider();
        var yarpResolver = CreateResolver(services);

        var destinationConfigs = new Dictionary<string, DestinationConfig>
        {
            ["dest-a"] = new()
            {
                Address = "https://my-svc",
            },
        };

        var result = await yarpResolver.ResolveDestinationsAsync(destinationConfigs, CancellationToken.None);

        Assert.Equal(3, result.Destinations.Count);
        Assert.Collection(result.Destinations.Select(d => d.Value.Address),
            a => Assert.Equal("https://10.10.10.10:8888/", a),
            a => Assert.Equal("https://[::1]:9999/", a),
            a => Assert.Equal("https://127.0.0.1:7777/", a));
    }

    private sealed class FakeDnsClient : IDnsQuery
    {
        public Func<string, QueryType, QueryClass, CancellationToken, Task<IDnsQueryResponse>>? QueryAsyncFunc { get; set; }

        public IDnsQueryResponse Query(string query, QueryType queryType, QueryClass queryClass = QueryClass.IN) => throw new NotImplementedException();
        public IDnsQueryResponse Query(DnsQuestion question) => throw new NotImplementedException();
        public IDnsQueryResponse Query(DnsQuestion question, DnsQueryAndServerOptions queryOptions) => throw new NotImplementedException();
        public Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default)
            => QueryAsyncFunc!(query, queryType, queryClass, cancellationToken);
        public Task<IDnsQueryResponse> QueryAsync(DnsQuestion question, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IDnsQueryResponse> QueryAsync(DnsQuestion question, DnsQueryAndServerOptions queryOptions, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IDnsQueryResponse QueryCache(DnsQuestion question) => throw new NotImplementedException();
        public IDnsQueryResponse QueryCache(string query, QueryType queryType, QueryClass queryClass = QueryClass.IN) => throw new NotImplementedException();
        public IDnsQueryResponse QueryReverse(IPAddress ipAddress) => throw new NotImplementedException();
        public IDnsQueryResponse QueryReverse(IPAddress ipAddress, DnsQueryAndServerOptions queryOptions) => throw new NotImplementedException();
        public Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress, DnsQueryAndServerOptions queryOptions, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IDnsQueryResponse QueryServer(IReadOnlyCollection<NameServer> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN) => throw new NotImplementedException();
        public IDnsQueryResponse QueryServer(IReadOnlyCollection<NameServer> servers, DnsQuestion question) => throw new NotImplementedException();
        public IDnsQueryResponse QueryServer(IReadOnlyCollection<NameServer> servers, DnsQuestion question, DnsQueryOptions queryOptions) => throw new NotImplementedException();
        public IDnsQueryResponse QueryServer(IReadOnlyCollection<IPEndPoint> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN) => throw new NotImplementedException();
        public IDnsQueryResponse QueryServer(IReadOnlyCollection<IPAddress> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN) => throw new NotImplementedException();
        public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<NameServer> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<NameServer> servers, DnsQuestion question, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<NameServer> servers, DnsQuestion question, DnsQueryOptions queryOptions, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<IPAddress> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<IPEndPoint> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<IPAddress> servers, IPAddress ipAddress) => throw new NotImplementedException();
        public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<IPEndPoint> servers, IPAddress ipAddress) => throw new NotImplementedException();
        public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress) => throw new NotImplementedException();
        public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress, DnsQueryOptions queryOptions) => throw new NotImplementedException();
        public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<IPAddress> servers, IPAddress ipAddress, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<IPEndPoint> servers, IPAddress ipAddress, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress, DnsQueryOptions queryOptions, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeDnsQueryResponse : IDnsQueryResponse
    {
        public IReadOnlyList<DnsQuestion>? Questions { get; set; }
        public IReadOnlyList<DnsResourceRecord>? Additionals { get; set; }
        public IEnumerable<DnsResourceRecord>? AllRecords { get; set; }
        public IReadOnlyList<DnsResourceRecord>? Answers { get; set; }
        public IReadOnlyList<DnsResourceRecord>? Authorities { get; set; }
        public string? AuditTrail { get; set; }
        public string? ErrorMessage { get; set; }
        public bool HasError { get; set; }
        public DnsResponseHeader? Header { get; set; }
        public int MessageSize { get; set; }
        public NameServer? NameServer { get; set; }
        public DnsQuerySettings? Settings { get; set; }
    }
}

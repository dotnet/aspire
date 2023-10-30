// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using DnsClient;
using DnsClient.Protocol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;
using Xunit;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Tests;

/// <summary>
/// Tests for <see cref="DnsServiceEndPointResolverBase"/> and <see cref="DnsSrvServiceEndPointResolverProvider"/>.
/// These also cover <see cref="ServiceEndPointResolver"/> and <see cref="ServiceEndPointResolverFactory"/> by extension.
/// </summary>
public class DnsSrvServiceEndPointResolverTests
{
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

    [Fact]
    public async Task ResolveServiceEndPoint_Dns()
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
                        new CNameRecord(new ResourceRecordInfo("srv-c", ResourceRecordType.AAAA, queryClass, 64, 0), DnsString.Parse("remotehost"))
                    }
                };

                return Task.FromResult<IDnsQueryResponse>(response);
            }
        };
        var services = new ServiceCollection()
            .AddSingleton<IDnsQuery>(dnsClientMock)
            .AddServiceDiscoveryCore()
            .AddDnsSrvServiceEndPointResolver(options => options.QuerySuffix = ".ns")
            .BuildServiceProvider();
        var resolverFactory = services.GetRequiredService<ServiceEndPointResolverFactory>();
        ServiceEndPointResolver resolver;
        await using ((resolver = resolverFactory.CreateResolver("http://basket")).ConfigureAwait(false))
        {
            Assert.NotNull(resolver);
            var tcs = new TaskCompletionSource<ServiceEndPointResolverResult>();
            resolver.OnEndPointsUpdated = tcs.SetResult;
            resolver.Start();
            var initialResult = await tcs.Task.ConfigureAwait(false);
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            Assert.Equal(ResolutionStatus.Success, initialResult.Status);
            Assert.Equal(3, initialResult.EndPoints.Count);
            var eps = initialResult.EndPoints;
            Assert.Equal(new IPEndPoint(IPAddress.Parse("10.10.10.10"), 8888), eps[0].EndPoint);
            Assert.Equal(new IPEndPoint(IPAddress.IPv6Loopback, 9999), eps[1].EndPoint);
            Assert.Equal(new DnsEndPoint("remotehost", 7777), eps[2].EndPoint);

            Assert.All(initialResult.EndPoints, ep =>
            {
                var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                Assert.Null(hostNameFeature);
            });
        }
    }

    /// <summary>
    /// Tests that when there are multiple resolvers registered, they are consulted in registration order and each provider only adds endpoints if the providers before it did not.
    /// </summary>
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task ResolveServiceEndPoint_Dns_MultipleProviders_PreventMixing(bool dnsFirst)
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
                        new CNameRecord(new ResourceRecordInfo("srv-c", ResourceRecordType.AAAA, queryClass, 64, 0), DnsString.Parse("remotehost"))
                    }
                };

                return Task.FromResult<IDnsQueryResponse>(response);
            }
        };
        var configSource = new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>
            {
                ["services:basket:0"] = "localhost:8080",
                ["services:basket:1"] = "remotehost:9090",
            }
        };
        var config = new ConfigurationBuilder().Add(configSource);
        var serviceCollection = new ServiceCollection()
            .AddSingleton<IDnsQuery>(dnsClientMock)
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore();
        if (dnsFirst)
        {
            serviceCollection
            .AddDnsSrvServiceEndPointResolver(options =>
            {
                options.QuerySuffix = ".ns";
                options.ApplyHostNameMetadata = _ => true;
            })
            .AddConfigurationServiceEndPointResolver();
        }
        else
        {
            serviceCollection
            .AddConfigurationServiceEndPointResolver()
            .AddDnsSrvServiceEndPointResolver(options => options.QuerySuffix = ".ns");
        };
        var services = serviceCollection.BuildServiceProvider();
        var resolverFactory = services.GetRequiredService<ServiceEndPointResolverFactory>();
        ServiceEndPointResolver resolver;
        await using ((resolver = resolverFactory.CreateResolver("http://basket")).ConfigureAwait(false))
        {
            Assert.NotNull(resolver);
            var tcs = new TaskCompletionSource<ServiceEndPointResolverResult>();
            resolver.OnEndPointsUpdated = tcs.SetResult;
            resolver.Start();
            var initialResult = await tcs.Task.ConfigureAwait(false);
            Assert.NotNull(initialResult);
            Assert.Null(initialResult.Status.Exception);
            Assert.True(initialResult.ResolvedSuccessfully);
            Assert.Equal(ResolutionStatus.Success, initialResult.Status);

            if (dnsFirst)
            {
                // We expect only the results from the DNS provider.
                Assert.Equal(3, initialResult.EndPoints.Count);
                var eps = initialResult.EndPoints;
                Assert.Equal(new IPEndPoint(IPAddress.Parse("10.10.10.10"), 8888), eps[0].EndPoint);
                Assert.Equal(new IPEndPoint(IPAddress.IPv6Loopback, 9999), eps[1].EndPoint);
                Assert.Equal(new DnsEndPoint("remotehost", 7777), eps[2].EndPoint);

                Assert.All(initialResult.EndPoints, ep =>
                {
                    var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                    Assert.NotNull(hostNameFeature);
                    Assert.Equal("basket", hostNameFeature.HostName);
                });
            }
            else
            {
                // We expect only the results from the Configuration provider.
                Assert.Equal(2, initialResult.EndPoints.Count);
                Assert.Equal(new DnsEndPoint("localhost", 8080), initialResult.EndPoints[0].EndPoint);
                Assert.Equal(new DnsEndPoint("remotehost", 9090), initialResult.EndPoints[1].EndPoint);

                Assert.All(initialResult.EndPoints, ep =>
                {
                    var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                    Assert.Null(hostNameFeature);
                });
            }
        }
    }

    public class MyConfigurationProvider : ConfigurationProvider, IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder) => this;
        public void SetValues(IEnumerable<KeyValuePair<string, string?>> values)
        {
            Data.Clear();
            foreach (var (key, value) in values)
            {
                Data[key] = value;
            }

            OnReload();
        }
    }

    /*
    [Fact]
    public async Task ResolveServiceEndPoint_Dns_RespectsChangeToken()
    {
        var oneEndPoint = new Dictionary<string, string?>
        {
            ["services:basket:http:0:host"] = "localhost",
            ["services:basket:http:0:port"] = "8080",
        };
        var bothEndPoints = new Dictionary<string, string?>(oneEndPoint)
        {
            ["services:basket:http:1:host"] = "remotehost",
            ["services:basket:http:1:port"] = "9090",
        };
        var configSource = new MyConfigurationProvider();
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Add(configSource).Build())
            .AddServiceDiscovery()
            .AddConfigurationServiceEndPointResolver()
            .BuildServiceProvider();
        var resolverFactory = services.GetRequiredService<ServiceEndPointResolverFactory>();
        ServiceEndPointResolver resolver;
        await using ((resolver = resolverFactory.CreateResolver("http://basket")).ConfigureAwait(false))
        {
            Assert.NotNull(resolver);
            var channel = Channel.CreateUnbounded<ServiceEndPointResolverResult>();
            resolver.OnEndPointsUpdated = v => channel.Writer.TryWrite(v);
            resolver.Start();
            var initialResult = await channel.Reader.ReadAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.NotNull(initialResult);
            Assert.False(initialResult.ResolvedSuccessfully);
            Assert.Equal(ResolutionStatusCode.Error, initialResult.Status.StatusCode);
            Assert.Null(initialResult.EndPoints);

            // Update the config and check that it flows through the system.
            configSource.SetValues(oneEndPoint);

            // If we don't get an update relatively soon, something is broken. We add a timeout here because we don't want an issue to
            // cause an indefinite test hang. We expect the result to be published practically immediately, though.
            _ = await channel.Reader.ReadAsync(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            var oneEpResult = await resolver.GetEndPointsAsync(CancellationToken.None).ConfigureAwait(false);
            var firstEp = Assert.Single(oneEpResult);
            Assert.Equal(new DnsEndPoint("localhost", 8080), firstEp.EndPoint);

            // Do it again to check that an updated (not cached) version is published.
            configSource.SetValues(bothEndPoints);
            var twoEpResult = await channel.Reader.ReadAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.True(twoEpResult.ResolvedSuccessfully);
            Assert.Equal(2, twoEpResult.EndPoints.Count);
            Assert.Equal(new DnsEndPoint("localhost", 8080), twoEpResult.EndPoints[0].EndPoint);
            Assert.Equal(new DnsEndPoint("remotehost", 9090), twoEpResult.EndPoints[1].EndPoint);
        }
    }
    */
}

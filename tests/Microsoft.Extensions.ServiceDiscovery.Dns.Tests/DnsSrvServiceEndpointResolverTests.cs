// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;
using Microsoft.Extensions.ServiceDiscovery.Internal;
using Xunit;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Tests;

/// <summary>
/// Tests for <see cref="DnsServiceEndpointProviderBase"/> and <see cref="DnsSrvServiceEndpointProviderFactory"/>.
/// These also cover <see cref="ServiceEndpointWatcher"/> and <see cref="ServiceEndpointWatcherFactory"/> by extension.
/// </summary>
public class DnsSrvServiceEndpointResolverTests
{
    private sealed class FakeDnsResolver : IDnsResolver
    {
        public Func<string, AddressFamily, CancellationToken, ValueTask<AddressResult[]>>? ResolveIPAddressesAsyncFunc { get; set; }
        public ValueTask<AddressResult[]> ResolveIPAddressesAsync(string name, AddressFamily addressFamily, CancellationToken cancellationToken = default) => ResolveIPAddressesAsyncFunc!.Invoke(name, addressFamily, cancellationToken);

        public Func<string, CancellationToken, ValueTask<AddressResult[]>>? ResolveIPAddressesAsyncFunc2 { get; set; }

        public ValueTask<AddressResult[]> ResolveIPAddressesAsync(string name, CancellationToken cancellationToken = default) => ResolveIPAddressesAsyncFunc2!.Invoke(name, cancellationToken);

        public Func<string, CancellationToken, ValueTask<ServiceResult[]>>? ResolveServiceAsyncFunc { get; set; }

        public ValueTask<ServiceResult[]> ResolveServiceAsync(string name, CancellationToken cancellationToken = default) => ResolveServiceAsyncFunc!.Invoke(name, cancellationToken);
    }

    [Fact]
    public async Task ResolveServiceEndpoint_DnsSrv()
    {
        var dnsClientMock = new FakeDnsResolver
        {
            ResolveServiceAsyncFunc = (name, cancellationToken) =>
            {
                ServiceResult[] response = [
                    new ServiceResult(DateTime.UtcNow.AddSeconds(60), 99, 66, 8888, "srv-a", [new AddressResult(DateTime.UtcNow.AddSeconds(64), IPAddress.Parse("10.10.10.10"))]),
                    new ServiceResult(DateTime.UtcNow.AddSeconds(60), 99, 62, 9999, "srv-b", [new AddressResult(DateTime.UtcNow.AddSeconds(64), IPAddress.IPv6Loopback)]),
                    new ServiceResult(DateTime.UtcNow.AddSeconds(60), 99, 62, 7777, "srv-c", [])
                ];

                return ValueTask.FromResult(response);
            }
        };
        var services = new ServiceCollection()
            .AddSingleton<IDnsResolver>(dnsClientMock)
            .AddServiceDiscoveryCore()
            .AddDnsSrvServiceEndpointProvider(options => options.QuerySuffix = ".ns")
            .BuildServiceProvider();
        var watcherFactory = services.GetRequiredService<ServiceEndpointWatcherFactory>();
        ServiceEndpointWatcher watcher;
        await using ((watcher = watcherFactory.CreateWatcher("http://basket")).ConfigureAwait(false))
        {
            Assert.NotNull(watcher);
            var tcs = new TaskCompletionSource<ServiceEndpointResolverResult>();
            watcher.OnEndpointsUpdated = tcs.SetResult;
            watcher.Start();
            var initialResult = await tcs.Task;
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            Assert.Equal(3, initialResult.EndpointSource.Endpoints.Count);
            var eps = initialResult.EndpointSource.Endpoints;
            Assert.Equal(new IPEndPoint(IPAddress.Parse("10.10.10.10"), 8888), eps[0].EndPoint);
            Assert.Equal(new IPEndPoint(IPAddress.IPv6Loopback, 9999), eps[1].EndPoint);
            Assert.Equal(new DnsEndPoint("srv-c", 7777), eps[2].EndPoint);

            Assert.All(initialResult.EndpointSource.Endpoints, ep =>
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
    public async Task ResolveServiceEndpoint_DnsSrv_MultipleProviders_PreventMixing(bool dnsFirst)
    {
        var dnsClientMock = new FakeDnsResolver
        {
            ResolveServiceAsyncFunc = (name, cancellationToken) =>
            {
                ServiceResult[] response = [
                    new ServiceResult(DateTime.UtcNow.AddSeconds(60), 99, 66, 8888, "srv-a", [new AddressResult(DateTime.UtcNow.AddSeconds(64), IPAddress.Parse("10.10.10.10"))]),
                    new ServiceResult(DateTime.UtcNow.AddSeconds(60), 99, 62, 9999, "srv-b", [new AddressResult(DateTime.UtcNow.AddSeconds(64), IPAddress.IPv6Loopback)]),
                    new ServiceResult(DateTime.UtcNow.AddSeconds(60), 99, 62, 7777, "srv-c", [])
                ];

                return ValueTask.FromResult(response);
            }
        };
        var configSource = new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>
            {
                ["services:basket:http:0"] = "localhost:8080",
                ["services:basket:http:1"] = "remotehost:9090",
            }
        };
        var config = new ConfigurationBuilder().Add(configSource);
        var serviceCollection = new ServiceCollection()
            .AddSingleton<IDnsResolver>(dnsClientMock)
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore();
        if (dnsFirst)
        {
            serviceCollection
            .AddDnsSrvServiceEndpointProvider(options =>
            {
                options.QuerySuffix = ".ns";
                options.ShouldApplyHostNameMetadata = _ => true;
            })
            .AddConfigurationServiceEndpointProvider();
        }
        else
        {
            serviceCollection
            .AddConfigurationServiceEndpointProvider()
            .AddDnsSrvServiceEndpointProvider(options => options.QuerySuffix = ".ns");
        };
        var services = serviceCollection.BuildServiceProvider();
        var watcherFactory = services.GetRequiredService<ServiceEndpointWatcherFactory>();
        ServiceEndpointWatcher watcher;
        await using ((watcher = watcherFactory.CreateWatcher("http://basket")).ConfigureAwait(false))
        {
            Assert.NotNull(watcher);
            var tcs = new TaskCompletionSource<ServiceEndpointResolverResult>();
            watcher.OnEndpointsUpdated = tcs.SetResult;
            watcher.Start();
            var initialResult = await tcs.Task;
            Assert.NotNull(initialResult);
            Assert.Null(initialResult.Exception);
            Assert.True(initialResult.ResolvedSuccessfully);

            if (dnsFirst)
            {
                // We expect only the results from the DNS provider.
                Assert.Equal(3, initialResult.EndpointSource.Endpoints.Count);
                var eps = initialResult.EndpointSource.Endpoints;
                Assert.Equal(new IPEndPoint(IPAddress.Parse("10.10.10.10"), 8888), eps[0].EndPoint);
                Assert.Equal(new IPEndPoint(IPAddress.IPv6Loopback, 9999), eps[1].EndPoint);
                Assert.Equal(new DnsEndPoint("srv-c", 7777), eps[2].EndPoint);

                Assert.All(initialResult.EndpointSource.Endpoints, ep =>
                {
                    var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                    Assert.NotNull(hostNameFeature);
                    Assert.Equal("basket", hostNameFeature.HostName);
                });
            }
            else
            {
                // We expect only the results from the Configuration provider.
                Assert.Equal(2, initialResult.EndpointSource.Endpoints.Count);
                Assert.Equal(new DnsEndPoint("localhost", 8080), initialResult.EndpointSource.Endpoints[0].EndPoint);
                Assert.Equal(new DnsEndPoint("remotehost", 9090), initialResult.EndpointSource.Endpoints[1].EndPoint);

                Assert.All(initialResult.EndpointSource.Endpoints, ep =>
                {
                    var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                    Assert.Null(hostNameFeature);
                });
            }
        }
    }
}

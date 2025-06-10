// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Yarp.ReverseProxy.Configuration;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

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
        DnsResolver resolver = new DnsResolver(TimeProvider.System, NullLogger<DnsResolver>.Instance);

        await using var services = new ServiceCollection()
            .AddSingleton<IDnsResolver>(resolver)
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
        var dnsClientMock = new FakeDnsResolver
        {
            ResolveServiceAsyncFunc = (name, cancellationToken) =>
            {
                ServiceResult[] response = [
                    new ServiceResult(DateTime.UtcNow.AddSeconds(60), 99, 66, 8888, "srv-a", [new AddressResult(DateTime.UtcNow.AddSeconds(64), IPAddress.Parse("10.10.10.10"))]),
                    new ServiceResult(DateTime.UtcNow.AddSeconds(60), 99, 62, 9999, "srv-b", [new AddressResult(DateTime.UtcNow.AddSeconds(64), IPAddress.IPv6Loopback)]),
                    new ServiceResult(DateTime.UtcNow.AddSeconds(60), 99, 62, 7777, "srv-c", [new AddressResult(DateTime.UtcNow.AddSeconds(64), IPAddress.Loopback)])
                ];

                return ValueTask.FromResult(response);
            }
        };

        await using var services = new ServiceCollection()
            .AddSingleton<IDnsResolver>(dnsClientMock)
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

    private sealed class FakeDnsResolver : IDnsResolver
    {
        public Func<string, AddressFamily, CancellationToken, ValueTask<AddressResult[]>>? ResolveIPAddressesAsyncFunc { get; set; }
        public ValueTask<AddressResult[]> ResolveIPAddressesAsync(string name, AddressFamily addressFamily, CancellationToken cancellationToken = default) => ResolveIPAddressesAsyncFunc!.Invoke(name, addressFamily, cancellationToken);

        public Func<string, CancellationToken, ValueTask<AddressResult[]>>? ResolveIPAddressesAsyncFunc2 { get; set; }

        public ValueTask<AddressResult[]> ResolveIPAddressesAsync(string name, CancellationToken cancellationToken = default) => ResolveIPAddressesAsyncFunc2!.Invoke(name, cancellationToken);

        public Func<string, CancellationToken, ValueTask<ServiceResult[]>>? ResolveServiceAsyncFunc { get; set; }

        public ValueTask<ServiceResult[]> ResolveServiceAsync(string name, CancellationToken cancellationToken = default) => ResolveServiceAsyncFunc!.Invoke(name, cancellationToken);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ServiceDiscovery.Internal;
using Microsoft.Extensions.ServiceDiscovery.PassThrough;
using Xunit;

namespace Microsoft.Extensions.ServiceDiscovery.Tests;

/// <summary>
/// Tests for <see cref="PassThroughServiceEndPointResolverProvider"/>.
/// These also cover <see cref="ServiceEndPointWatcher"/> and <see cref="ServiceEndPointWatcherFactory"/> by extension.
/// </summary>
public class PassThroughServiceEndPointResolverTests
{
    [Fact]
    public async Task ResolveServiceEndPoint_PassThrough()
    {
        var services = new ServiceCollection()
            .AddServiceDiscoveryCore()
            .AddPassThroughServiceEndPointResolver()
            .BuildServiceProvider();
        var resolverFactory = services.GetRequiredService<ServiceEndPointWatcherFactory>();
        ServiceEndPointWatcher resolver;
        await using ((resolver = resolverFactory.CreateWatcher("http://basket")).ConfigureAwait(false))
        {
            Assert.NotNull(resolver);
            var tcs = new TaskCompletionSource<ServiceEndPointResolverResult>();
            resolver.OnEndPointsUpdated = tcs.SetResult;
            resolver.Start();
            var initialResult = await tcs.Task.ConfigureAwait(false);
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            var ep = Assert.Single(initialResult.EndPointSource.EndPoints);
            Assert.Equal(new DnsEndPoint("basket", 80), ep.EndPoint);
        }
    }

    [Fact]
    public async Task ResolveServiceEndPoint_Superseded()
    {
        var configSource = new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>
            {
                ["services:basket:http:0"] = "http://localhost:8080",
            }
        };
        var config = new ConfigurationBuilder().Add(configSource);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscovery() // Adds the configuration and pass-through providers.
            .BuildServiceProvider();
        var resolverFactory = services.GetRequiredService<ServiceEndPointWatcherFactory>();
        ServiceEndPointWatcher resolver;
        await using ((resolver = resolverFactory.CreateWatcher("http://basket")).ConfigureAwait(false))
        {
            Assert.NotNull(resolver);
            var tcs = new TaskCompletionSource<ServiceEndPointResolverResult>();
            resolver.OnEndPointsUpdated = tcs.SetResult;
            resolver.Start();
            var initialResult = await tcs.Task.ConfigureAwait(false);
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);

            // We expect the basket service to be resolved from Configuration, not the pass-through provider.
            Assert.Single(initialResult.EndPointSource.EndPoints);
            Assert.Equal(new UriEndPoint(new Uri("http://localhost:8080")), initialResult.EndPointSource.EndPoints[0].EndPoint);
        }
    }

    [Fact]
    public async Task ResolveServiceEndPoint_Fallback()
    {
        var configSource = new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>
            {
                ["services:basket:default:0"] = "http://localhost:8080",
            }
        };
        var config = new ConfigurationBuilder().Add(configSource);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscovery() // Adds the configuration and pass-through providers.
            .BuildServiceProvider();
        var resolverFactory = services.GetRequiredService<ServiceEndPointWatcherFactory>();
        ServiceEndPointWatcher resolver;
        await using ((resolver = resolverFactory.CreateWatcher("http://catalog")).ConfigureAwait(false))
        {
            Assert.NotNull(resolver);
            var tcs = new TaskCompletionSource<ServiceEndPointResolverResult>();
            resolver.OnEndPointsUpdated = tcs.SetResult;
            resolver.Start();
            var initialResult = await tcs.Task.ConfigureAwait(false);
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);

            // We expect the CATALOG service to be resolved from the pass-through provider.
            Assert.Single(initialResult.EndPointSource.EndPoints);
            Assert.Equal(new DnsEndPoint("catalog", 80), initialResult.EndPointSource.EndPoints[0].EndPoint);
        }
    }

    // Ensures that pass-through resolution succeeds in scenarios where no scheme is specified during resolution.
    [Fact]
    public async Task ResolveServiceEndPoint_Fallback_NoScheme()
    {
        var configSource = new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>
            {
                ["services:basket:default:0"] = "http://localhost:8080",
            }
        };
        var config = new ConfigurationBuilder().Add(configSource);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscovery() // Adds the configuration and pass-through providers.
            .BuildServiceProvider();

        var resolver = services.GetRequiredService<ServiceEndPointResolver>();
        var result = await resolver.GetEndPointsAsync("catalog", default);
        Assert.Equal(new DnsEndPoint("catalog", 0), result.EndPoints[0].EndPoint);
    }
}

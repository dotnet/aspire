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
/// Tests for <see cref="PassThroughServiceEndpointProviderFactory"/>.
/// These also cover <see cref="ServiceEndpointWatcher"/> and <see cref="ServiceEndpointWatcherFactory"/> by extension.
/// </summary>
public class PassThroughServiceEndpointResolverTests
{
    [Fact]
    public async Task ResolveServiceEndpoint_PassThrough()
    {
        var services = new ServiceCollection()
            .AddServiceDiscoveryCore()
            .AddPassThroughServiceEndpointProvider()
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
            var ep = Assert.Single(initialResult.EndpointSource.Endpoints);
            Assert.Equal(new DnsEndPoint("basket", 80), ep.EndPoint);
        }
    }

    [Fact]
    public async Task ResolveServiceEndpoint_Superseded()
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

            // We expect the basket service to be resolved from Configuration, not the pass-through provider.
            Assert.Single(initialResult.EndpointSource.Endpoints);
            Assert.Equal(new UriEndPoint(new Uri("http://localhost:8080")), initialResult.EndpointSource.Endpoints[0].EndPoint);
        }
    }

    [Fact]
    public async Task ResolveServiceEndpoint_Fallback()
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
        var watcherFactory = services.GetRequiredService<ServiceEndpointWatcherFactory>();
        ServiceEndpointWatcher watcher;
        await using ((watcher = watcherFactory.CreateWatcher("http://catalog")).ConfigureAwait(false))
        {
            Assert.NotNull(watcher);
            var tcs = new TaskCompletionSource<ServiceEndpointResolverResult>();
            watcher.OnEndpointsUpdated = tcs.SetResult;
            watcher.Start();
            var initialResult = await tcs.Task;
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);

            // We expect the CATALOG service to be resolved from the pass-through provider.
            Assert.Single(initialResult.EndpointSource.Endpoints);
            Assert.Equal(new DnsEndPoint("catalog", 80), initialResult.EndpointSource.Endpoints[0].EndPoint);
        }
    }

    // Ensures that pass-through resolution succeeds in scenarios where no scheme is specified during resolution.
    [Fact]
    public async Task ResolveServiceEndpoint_Fallback_NoScheme()
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

        var resolver = services.GetRequiredService<ServiceEndpointResolver>();
        var result = await resolver.GetEndpointsAsync("catalog", TestContext.Current.CancellationToken);
        Assert.Equal(new DnsEndPoint("catalog", 0), result.Endpoints[0].EndPoint);
    }
}

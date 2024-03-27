// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ServiceDiscovery.Configuration;
using Microsoft.Extensions.ServiceDiscovery.Internal;
using Xunit;

namespace Microsoft.Extensions.ServiceDiscovery.Tests;

/// <summary>
/// Tests for <see cref="ConfigurationServiceEndPointResolver"/>.
/// These also cover <see cref="ServiceEndPointWatcher"/> and <see cref="ServiceEndPointWatcherFactory"/> by extension.
/// </summary>
public class ConfigurationServiceEndPointResolverTests
{
    [Fact]
    public async Task ResolveServiceEndPoint_Configuration_SingleResult()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["services:basket:http"] = "localhost:8080",
        });
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore()
            .AddConfigurationServiceEndPointResolver()
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
            Assert.Equal(new DnsEndPoint("localhost", 8080), ep.EndPoint);

            Assert.All(initialResult.EndPointSource.EndPoints, ep =>
            {
                var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                Assert.Null(hostNameFeature);
            });
        }
    }

    [Fact]
    public async Task ResolveServiceEndPoint_Configuration_DisallowedScheme()
    {
        // Try to resolve an http endpoint when only https is allowed.
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["services:basket:foo:0"] = "http://localhost:8080",
            ["services:basket:foo:1"] = "https://localhost",
        });
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore()
            .AddConfigurationServiceEndPointResolver()
            .Configure<ServiceDiscoveryOptions>(o => o.AllowedSchemes = ["https"])
            .BuildServiceProvider();
        var resolverFactory = services.GetRequiredService<ServiceEndPointWatcherFactory>();
        ServiceEndPointWatcher resolver;

        // Explicitly specifying http.
        // We should get no endpoint back because http is not allowed by configuration.
        await using ((resolver = resolverFactory.CreateWatcher("http://_foo.basket")).ConfigureAwait(false))
        {
            Assert.NotNull(resolver);
            var tcs = new TaskCompletionSource<ServiceEndPointResolverResult>();
            resolver.OnEndPointsUpdated = tcs.SetResult;
            resolver.Start();
            var initialResult = await tcs.Task.ConfigureAwait(false);
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            Assert.Empty(initialResult.EndPointSource.EndPoints);
        }

        // Specifying either https or http.
        // The result should be that we only get the http endpoint back.
        await using ((resolver = resolverFactory.CreateWatcher("https+http://_foo.basket")).ConfigureAwait(false))
        {
            Assert.NotNull(resolver);
            var tcs = new TaskCompletionSource<ServiceEndPointResolverResult>();
            resolver.OnEndPointsUpdated = tcs.SetResult;
            resolver.Start();
            var initialResult = await tcs.Task.ConfigureAwait(false);
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            var ep = Assert.Single(initialResult.EndPointSource.EndPoints);
            Assert.Equal(new UriEndPoint(new Uri("https://localhost")), ep.EndPoint);
        }

        // Specifying either https or http, but in reverse.
        // The result should be that we only get the http endpoint back.
        await using ((resolver = resolverFactory.CreateWatcher("http+https://_foo.basket")).ConfigureAwait(false))
        {
            Assert.NotNull(resolver);
            var tcs = new TaskCompletionSource<ServiceEndPointResolverResult>();
            resolver.OnEndPointsUpdated = tcs.SetResult;
            resolver.Start();
            var initialResult = await tcs.Task.ConfigureAwait(false);
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            var ep = Assert.Single(initialResult.EndPointSource.EndPoints);
            Assert.Equal(new UriEndPoint(new Uri("https://localhost")), ep.EndPoint);
        }
    }

    [Fact]
    public async Task ResolveServiceEndPoint_Configuration_MultipleResults()
    {
        var configSource = new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>
            {
                ["services:basket:http:0"] = "http://localhost:8080",
                ["services:basket:http:1"] = "http://remotehost:9090",
            }
        };
        var config = new ConfigurationBuilder().Add(configSource);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore()
            .AddConfigurationServiceEndPointResolver(options => options.ApplyHostNameMetadata = _ => true)
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
            Assert.Equal(2, initialResult.EndPointSource.EndPoints.Count);
            Assert.Equal(new UriEndPoint(new Uri("http://localhost:8080")), initialResult.EndPointSource.EndPoints[0].EndPoint);
            Assert.Equal(new UriEndPoint(new Uri("http://remotehost:9090")), initialResult.EndPointSource.EndPoints[1].EndPoint);

            Assert.All(initialResult.EndPointSource.EndPoints, ep =>
            {
                var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                Assert.NotNull(hostNameFeature);
                Assert.Equal("basket", hostNameFeature.HostName);
            });
        }

        // Request either https or http. Since there are only http endpoints, we should get only http endpoints back.
        await using ((resolver = resolverFactory.CreateWatcher("https+http://basket")).ConfigureAwait(false))
        {
            Assert.NotNull(resolver);
            var tcs = new TaskCompletionSource<ServiceEndPointResolverResult>();
            resolver.OnEndPointsUpdated = tcs.SetResult;
            resolver.Start();
            var initialResult = await tcs.Task.ConfigureAwait(false);
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            Assert.Equal(2, initialResult.EndPointSource.EndPoints.Count);
            Assert.Equal(new UriEndPoint(new Uri("http://localhost:8080")), initialResult.EndPointSource.EndPoints[0].EndPoint);
            Assert.Equal(new UriEndPoint(new Uri("http://remotehost:9090")), initialResult.EndPointSource.EndPoints[1].EndPoint);

            Assert.All(initialResult.EndPointSource.EndPoints, ep =>
            {
                var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                Assert.NotNull(hostNameFeature);
                Assert.Equal("basket", hostNameFeature.HostName);
            });
        }
    }

    [Fact]
    public async Task ResolveServiceEndPoint_Configuration_MultipleProtocols()
    {
        var configSource = new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>
            {
                ["services:basket:http:0"] = "http://localhost:8080",
                ["services:basket:https:1"] = "https://remotehost:9090",
                ["services:basket:grpc:0"] = "localhost:2222",
                ["services:basket:grpc:1"] = "127.0.0.1:3333",
                ["services:basket:grpc:2"] = "http://remotehost:4444",
                ["services:basket:grpc:3"] = "https://remotehost:5555",
            }
        };
        var config = new ConfigurationBuilder().Add(configSource);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore()
            .AddConfigurationServiceEndPointResolver()
            .BuildServiceProvider();
        var resolverFactory = services.GetRequiredService<ServiceEndPointWatcherFactory>();
        ServiceEndPointWatcher resolver;
        await using ((resolver = resolverFactory.CreateWatcher("http://_grpc.basket")).ConfigureAwait(false))
        {
            Assert.NotNull(resolver);
            var tcs = new TaskCompletionSource<ServiceEndPointResolverResult>();
            resolver.OnEndPointsUpdated = tcs.SetResult;
            resolver.Start();
            var initialResult = await tcs.Task.ConfigureAwait(false);
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            Assert.Equal(3, initialResult.EndPointSource.EndPoints.Count);
            Assert.Equal(new DnsEndPoint("localhost", 2222), initialResult.EndPointSource.EndPoints[0].EndPoint);
            Assert.Equal(new IPEndPoint(IPAddress.Loopback, 3333), initialResult.EndPointSource.EndPoints[1].EndPoint);
            Assert.Equal(new UriEndPoint(new Uri("http://remotehost:4444")), initialResult.EndPointSource.EndPoints[2].EndPoint);

            Assert.All(initialResult.EndPointSource.EndPoints, ep =>
            {
                var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                Assert.Null(hostNameFeature);
            });
        }
    }

    [Fact]
    public async Task ResolveServiceEndPoint_Configuration_MultipleProtocols_WithSpecificationByConsumer()
    {
        var configSource = new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>
            {
                ["services:basket:default:0"] = "http://localhost:8080",
                ["services:basket:default:1"] = "remotehost:9090",
                ["services:basket:grpc:0"] = "localhost:2222",
                ["services:basket:grpc:1"] = "127.0.0.1:3333",
                ["services:basket:grpc:2"] = "http://remotehost:4444",
                ["services:basket:grpc:3"] = "https://remotehost:5555",
            }
        };
        var config = new ConfigurationBuilder().Add(configSource);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore()
            .AddConfigurationServiceEndPointResolver()
            .BuildServiceProvider();
        var resolverFactory = services.GetRequiredService<ServiceEndPointWatcherFactory>();
        ServiceEndPointWatcher resolver;
        await using ((resolver = resolverFactory.CreateWatcher("https+http://_grpc.basket")).ConfigureAwait(false))
        {
            Assert.NotNull(resolver);
            var tcs = new TaskCompletionSource<ServiceEndPointResolverResult>();
            resolver.OnEndPointsUpdated = tcs.SetResult;
            resolver.Start();
            var initialResult = await tcs.Task.ConfigureAwait(false);
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            Assert.Equal(3, initialResult.EndPointSource.EndPoints.Count);

            // These must be treated as HTTPS by the HttpClient middleware, but that is not the responsibility of the resolver.
            Assert.Equal(new DnsEndPoint("localhost", 2222), initialResult.EndPointSource.EndPoints[0].EndPoint);
            Assert.Equal(new IPEndPoint(IPAddress.Loopback, 3333), initialResult.EndPointSource.EndPoints[1].EndPoint);

            // We expect the HTTPS endpoint back but not the HTTP one.
            Assert.Equal(new UriEndPoint(new Uri("https://remotehost:5555")), initialResult.EndPointSource.EndPoints[2].EndPoint);

            Assert.All(initialResult.EndPointSource.EndPoints, ep =>
            {
                var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                Assert.Null(hostNameFeature);
            });
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
}

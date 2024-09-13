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
/// Tests for <see cref="ConfigurationServiceEndpointProvider"/>.
/// These also cover <see cref="ServiceEndpointWatcher"/> and <see cref="ServiceEndpointWatcherFactory"/> by extension.
/// </summary>
public class ConfigurationServiceEndpointResolverTests
{
    [Fact]
    public async Task ResolveServiceEndpoint_Configuration_SingleResult_NoScheme()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["services:basket:http"] = "localhost:8080",
        });
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore()
            .AddConfigurationServiceEndpointProvider()
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
            Assert.Equal(new DnsEndPoint("localhost", 8080), ep.EndPoint);

            Assert.All(initialResult.EndpointSource.Endpoints, ep =>
            {
                var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                Assert.Null(hostNameFeature);
            });
        }
    }

    [Fact]
    public async Task ResolveServiceEndpoint_Configuration_DisallowedScheme()
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
            .AddConfigurationServiceEndpointProvider()
            .Configure<ServiceDiscoveryOptions>(o =>
            {
                o.AllowAllSchemes = false;
                o.AllowedSchemes = ["https"];
            })
            .BuildServiceProvider();
        var watcherFactory = services.GetRequiredService<ServiceEndpointWatcherFactory>();
        ServiceEndpointWatcher watcher;

        // Explicitly specifying http.
        // We should get no endpoint back because http is not allowed by configuration.
        await using ((watcher = watcherFactory.CreateWatcher("http://_foo.basket")).ConfigureAwait(false))
        {
            Assert.NotNull(watcher);
            var tcs = new TaskCompletionSource<ServiceEndpointResolverResult>();
            watcher.OnEndpointsUpdated = tcs.SetResult;
            watcher.Start();
            var initialResult = await tcs.Task;
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            Assert.Empty(initialResult.EndpointSource.Endpoints);
        }

        // Specifying no scheme.
        // We should get the HTTPS endpoint back, since it is explicitly allowed
        await using ((watcher = watcherFactory.CreateWatcher("_foo.basket")).ConfigureAwait(false))
        {
            Assert.NotNull(watcher);
            var tcs = new TaskCompletionSource<ServiceEndpointResolverResult>();
            watcher.OnEndpointsUpdated = tcs.SetResult;
            watcher.Start();
            var initialResult = await tcs.Task;
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            var ep = Assert.Single(initialResult.EndpointSource.Endpoints);
            Assert.Equal(new UriEndPoint(new Uri("https://localhost")), ep.EndPoint);
        }

        // Specifying either https or http.
        // We should only get the https endpoint back.
        await using ((watcher = watcherFactory.CreateWatcher("https+http://_foo.basket")).ConfigureAwait(false))
        {
            Assert.NotNull(watcher);
            var tcs = new TaskCompletionSource<ServiceEndpointResolverResult>();
            watcher.OnEndpointsUpdated = tcs.SetResult;
            watcher.Start();
            var initialResult = await tcs.Task;
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            var ep = Assert.Single(initialResult.EndpointSource.Endpoints);
            Assert.Equal(new UriEndPoint(new Uri("https://localhost")), ep.EndPoint);
        }

        // Specifying either https or http, but in reverse.
        // We should only get the https endpoint back.
        await using ((watcher = watcherFactory.CreateWatcher("http+https://_foo.basket")).ConfigureAwait(false))
        {
            Assert.NotNull(watcher);
            var tcs = new TaskCompletionSource<ServiceEndpointResolverResult>();
            watcher.OnEndpointsUpdated = tcs.SetResult;
            watcher.Start();
            var initialResult = await tcs.Task;
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            var ep = Assert.Single(initialResult.EndpointSource.Endpoints);
            Assert.Equal(new UriEndPoint(new Uri("https://localhost")), ep.EndPoint);
        }
    }

    [Fact]
    public async Task ResolveServiceEndpoint_Configuration_DefaultEndpointName()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["services:basket:default:0"] = "https://localhost:8080",
            ["services:basket:otlp:0"] = "https://localhost:8888",
        });
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore()
            .AddConfigurationServiceEndpointProvider(o =>
            {
                o.ShouldApplyHostNameMetadata = _ => true;
            })
            .Configure<ServiceDiscoveryOptions>(o =>
            {
                o.AllowAllSchemes = false;
                o.AllowedSchemes = ["https"];
            })
            .BuildServiceProvider();
        var watcherFactory = services.GetRequiredService<ServiceEndpointWatcherFactory>();
        ServiceEndpointWatcher watcher;

        // Explicitly specifying https as the scheme, but the endpoint section in configuration is the default value ("default").
        // We should get the endpoint back because it is an https endpoint (allowed) with the default endpoint name.
        await using ((watcher = watcherFactory.CreateWatcher("https://basket")).ConfigureAwait(false))
        {
            Assert.NotNull(watcher);
            var tcs = new TaskCompletionSource<ServiceEndpointResolverResult>();
            watcher.OnEndpointsUpdated = tcs.SetResult;
            watcher.Start();
            var initialResult = await tcs.Task;
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            Assert.Single(initialResult.EndpointSource.Endpoints);
            Assert.Equal(new UriEndPoint(new Uri("https://localhost:8080")), initialResult.EndpointSource.Endpoints[0].EndPoint);

            Assert.All(initialResult.EndpointSource.Endpoints, ep =>
            {
                var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                Assert.NotNull(hostNameFeature);
                Assert.Equal("basket", hostNameFeature.HostName);
            });
        }

        // Not specifying the scheme or endpoint name.
        // We should get the endpoint back because it is an https endpoint (allowed) with the default endpoint name.
        await using ((watcher = watcherFactory.CreateWatcher("basket")).ConfigureAwait(false))
        {
            Assert.NotNull(watcher);
            var tcs = new TaskCompletionSource<ServiceEndpointResolverResult>();
            watcher.OnEndpointsUpdated = tcs.SetResult;
            watcher.Start();
            var initialResult = await tcs.Task;
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            Assert.Single(initialResult.EndpointSource.Endpoints);
            Assert.Equal(new UriEndPoint(new Uri("https://localhost:8080")), initialResult.EndpointSource.Endpoints[0].EndPoint);
        }

        // Not specifying the scheme, but specifying the default endpoint name.
        // We should get the endpoint back because it is an https endpoint (allowed) with the default endpoint name.
        await using ((watcher = watcherFactory.CreateWatcher("_default.basket")).ConfigureAwait(false))
        {
            Assert.NotNull(watcher);
            var tcs = new TaskCompletionSource<ServiceEndpointResolverResult>();
            watcher.OnEndpointsUpdated = tcs.SetResult;
            watcher.Start();
            var initialResult = await tcs.Task;
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            Assert.Single(initialResult.EndpointSource.Endpoints);
            Assert.Equal(new UriEndPoint(new Uri("https://localhost:8080")), initialResult.EndpointSource.Endpoints[0].EndPoint);
        }
    }

    /// <summary>
    /// Checks that when there is no named endpoint, configuration resolves first from the "default" section, then sections named by the scheme names.
    /// </summary>
    [Theory]
    [InlineData(true, true, "https://basket", "https://default-host:8080")]
    [InlineData(false, true, "https://basket","https://https-host:8080")]
    [InlineData(true, false, "https://basket", "https://default-host:8080")]
    [InlineData(true, true, "basket", "https://default-host:8080")]
    [InlineData(false, true, "basket", null)]
    [InlineData(true, false, "basket", "https://default-host:8080")]
    [InlineData(true, true, "http+https://basket", "https://default-host:8080")]
    [InlineData(false, true, "http+https://basket","https://https-host:8080")]
    [InlineData(true, false, "http+https://basket", "https://default-host:8080")]
    public async Task ResolveServiceEndpoint_Configuration_DefaultEndpointName_ResolutionOrder(
        bool includeDefault,
        bool includeSchemeNamed,
        string serviceName,
        string? expectedResult)
    {
        var data = new Dictionary<string, string?>();
        if (includeDefault)
        {
            data["services:basket:default:0"] = "https://default-host:8080";
        }

        if (includeSchemeNamed)
        {
            data["services:basket:https:0"] = "https://https-host:8080";
        }

        var config = new ConfigurationBuilder().AddInMemoryCollection(data);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore()
            .AddConfigurationServiceEndpointProvider()
            .BuildServiceProvider();
        var watcherFactory = services.GetRequiredService<ServiceEndpointWatcherFactory>();
        ServiceEndpointWatcher watcher;

        // Scheme in query
        await using ((watcher = watcherFactory.CreateWatcher(serviceName)).ConfigureAwait(false))
        {
            Assert.NotNull(watcher);
            var tcs = new TaskCompletionSource<ServiceEndpointResolverResult>();
            watcher.OnEndpointsUpdated = tcs.SetResult;
            watcher.Start();
            var initialResult = await tcs.Task;
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            if (expectedResult is not null)
            {
                Assert.Single(initialResult.EndpointSource.Endpoints);
                Assert.Equal(new UriEndPoint(new Uri(expectedResult)), initialResult.EndpointSource.Endpoints[0].EndPoint);
            }
            else
            {
                Assert.Empty(initialResult.EndpointSource.Endpoints);
            }
        }
    }

    [Fact]
    public async Task ResolveServiceEndpoint_Configuration_MultipleResults()
    {
        var configSource = new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>
            {
                ["services:basket:default:0"] = "http://localhost:8080",
                ["services:basket:default:1"] = "http://remotehost:9090",
            }
        };
        var config = new ConfigurationBuilder().Add(configSource);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore()
            .AddConfigurationServiceEndpointProvider(options => options.ShouldApplyHostNameMetadata = _ => true)
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
            Assert.Equal(2, initialResult.EndpointSource.Endpoints.Count);
            Assert.Equal(new UriEndPoint(new Uri("http://localhost:8080")), initialResult.EndpointSource.Endpoints[0].EndPoint);
            Assert.Equal(new UriEndPoint(new Uri("http://remotehost:9090")), initialResult.EndpointSource.Endpoints[1].EndPoint);

            Assert.All(initialResult.EndpointSource.Endpoints, ep =>
            {
                var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                Assert.NotNull(hostNameFeature);
                Assert.Equal("basket", hostNameFeature.HostName);
            });
        }

        // Request either https or http. Since there are only http endpoints, we should get only http endpoints back.
        await using ((watcher = watcherFactory.CreateWatcher("https+http://basket")).ConfigureAwait(false))
        {
            Assert.NotNull(watcher);
            var tcs = new TaskCompletionSource<ServiceEndpointResolverResult>();
            watcher.OnEndpointsUpdated = tcs.SetResult;
            watcher.Start();
            var initialResult = await tcs.Task;
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            Assert.Equal(2, initialResult.EndpointSource.Endpoints.Count);
            Assert.Equal(new UriEndPoint(new Uri("http://localhost:8080")), initialResult.EndpointSource.Endpoints[0].EndPoint);
            Assert.Equal(new UriEndPoint(new Uri("http://remotehost:9090")), initialResult.EndpointSource.Endpoints[1].EndPoint);

            Assert.All(initialResult.EndpointSource.Endpoints, ep =>
            {
                var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                Assert.NotNull(hostNameFeature);
                Assert.Equal("basket", hostNameFeature.HostName);
            });
        }
    }

    [Fact]
    public async Task ResolveServiceEndpoint_Configuration_MultipleProtocols()
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
            .AddConfigurationServiceEndpointProvider()
            .BuildServiceProvider();
        var watcherFactory = services.GetRequiredService<ServiceEndpointWatcherFactory>();
        ServiceEndpointWatcher watcher;
        await using ((watcher = watcherFactory.CreateWatcher("http://_grpc.basket")).ConfigureAwait(false))
        {
            Assert.NotNull(watcher);
            var tcs = new TaskCompletionSource<ServiceEndpointResolverResult>();
            watcher.OnEndpointsUpdated = tcs.SetResult;
            watcher.Start();
            var initialResult = await tcs.Task;
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            Assert.Equal(3, initialResult.EndpointSource.Endpoints.Count);
            Assert.Equal(new DnsEndPoint("localhost", 2222), initialResult.EndpointSource.Endpoints[0].EndPoint);
            Assert.Equal(new IPEndPoint(IPAddress.Loopback, 3333), initialResult.EndpointSource.Endpoints[1].EndPoint);
            Assert.Equal(new UriEndPoint(new Uri("http://remotehost:4444")), initialResult.EndpointSource.Endpoints[2].EndPoint);

            Assert.All(initialResult.EndpointSource.Endpoints, ep =>
            {
                var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                Assert.Null(hostNameFeature);
            });
        }
    }

    [Fact]
    public async Task ResolveServiceEndpoint_Configuration_MultipleProtocols_WithSpecificationByConsumer()
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
            .AddConfigurationServiceEndpointProvider()
            .BuildServiceProvider();
        var watcherFactory = services.GetRequiredService<ServiceEndpointWatcherFactory>();
        ServiceEndpointWatcher watcher;
        await using ((watcher = watcherFactory.CreateWatcher("https+http://_grpc.basket")).ConfigureAwait(false))
        {
            Assert.NotNull(watcher);
            var tcs = new TaskCompletionSource<ServiceEndpointResolverResult>();
            watcher.OnEndpointsUpdated = tcs.SetResult;
            watcher.Start();
            var initialResult = await tcs.Task;
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            Assert.Equal(3, initialResult.EndpointSource.Endpoints.Count);

            // These must be treated as HTTPS by the HttpClient middleware, but that is not the responsibility of the resolver.
            Assert.Equal(new DnsEndPoint("localhost", 2222), initialResult.EndpointSource.Endpoints[0].EndPoint);
            Assert.Equal(new IPEndPoint(IPAddress.Loopback, 3333), initialResult.EndpointSource.Endpoints[1].EndPoint);

            // We expect the HTTPS endpoint back but not the HTTP one.
            Assert.Equal(new UriEndPoint(new Uri("https://remotehost:5555")), initialResult.EndpointSource.Endpoints[2].EndPoint);

            Assert.All(initialResult.EndpointSource.Endpoints, ep =>
            {
                var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                Assert.Null(hostNameFeature);
            });
        }
    }
}

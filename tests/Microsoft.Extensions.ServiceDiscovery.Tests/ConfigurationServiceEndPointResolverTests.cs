// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;
using Xunit;

namespace Microsoft.Extensions.ServiceDiscovery.Tests;

/// <summary>
/// Tests for <see cref="ConfigurationServiceEndPointResolver"/> and <see cref="ConfigurationServiceEndPointResolverProvider"/>.
/// These also cover <see cref="ServiceEndPointResolver"/> and <see cref="ServiceEndPointResolverFactory"/> by extension.
/// </summary>
public class ConfigurationServiceEndPointResolverTests
{
    [Fact]
    public async Task ResolveServiceEndPoint_Configuration_SingleResult()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["services:basket"] = "localhost:8080",
        });
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore()
            .AddConfigurationServiceEndPointResolver()
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
            var ep = Assert.Single(initialResult.EndPoints);
            Assert.Equal(new DnsEndPoint("localhost", 8080), ep.EndPoint);

            Assert.All(initialResult.EndPoints, ep =>
            {
                var hostNameFeature = ep.Features.Get<IHostNameFeature>();
                Assert.Null(hostNameFeature);
            });
        }
    }

    [Fact]
    public async Task ResolveServiceEndPoint_Configuration_MultipleResults()
    {
        var configSource = new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>
            {
                ["services:basket:0"] = "http://localhost:8080",
                ["services:basket:1"] = "http://remotehost:9090",
            }
        };
        var config = new ConfigurationBuilder().Add(configSource);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore()
            .AddConfigurationServiceEndPointResolver(options => options.ApplyHostNameMetadata = _ => true)
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
            Assert.Equal(2, initialResult.EndPoints.Count);
            Assert.Equal(new DnsEndPoint("localhost", 8080), initialResult.EndPoints[0].EndPoint);
            Assert.Equal(new DnsEndPoint("remotehost", 9090), initialResult.EndPoints[1].EndPoint);

            Assert.All(initialResult.EndPoints, ep =>
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
                ["services:basket:0"] = "http://localhost:8080",
                ["services:basket:1"] = "http://remotehost:9090",
                ["services:basket:2"] = "http://_grpc.localhost:2222",
                ["services:basket:3"] = "grpc://remotehost:2222",
            }
        };
        var config = new ConfigurationBuilder().Add(configSource);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config.Build())
            .AddServiceDiscoveryCore()
            .AddConfigurationServiceEndPointResolver()
            .BuildServiceProvider();
        var resolverFactory = services.GetRequiredService<ServiceEndPointResolverFactory>();
        ServiceEndPointResolver resolver;
        await using ((resolver = resolverFactory.CreateResolver("http://_grpc.basket")).ConfigureAwait(false))
        {
            Assert.NotNull(resolver);
            var tcs = new TaskCompletionSource<ServiceEndPointResolverResult>();
            resolver.OnEndPointsUpdated = tcs.SetResult;
            resolver.Start();
            var initialResult = await tcs.Task.ConfigureAwait(false);
            Assert.NotNull(initialResult);
            Assert.True(initialResult.ResolvedSuccessfully);
            Assert.Equal(ResolutionStatus.Success, initialResult.Status);
            Assert.Equal(2, initialResult.EndPoints.Count);
            Assert.Equal(new DnsEndPoint("localhost", 2222), initialResult.EndPoints[0].EndPoint);
            Assert.Equal(new DnsEndPoint("remotehost", 2222), initialResult.EndPoints[1].EndPoint);

            Assert.All(initialResult.EndPoints, ep =>
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

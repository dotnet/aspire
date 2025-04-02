// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.ServiceDiscovery.Tests;

#pragma warning disable IDE0200

public class ExtensionsServicePublicApiTests
{
    [Fact]
    public void AddServiceDiscoveryShouldThrowWhenHttpClientBuilderIsNull()
    {
        IHttpClientBuilder httpClientBuilder = null!;

        var action = () => httpClientBuilder.AddServiceDiscovery();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(httpClientBuilder), exception.ParamName);
    }

    [Fact]
    public void AddServiceDiscoveryShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        var action = () => services.AddServiceDiscovery();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(services), exception.ParamName);
    }

    [Fact]
    public void AddServiceDiscoveryWithConfigureOptionsShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;
        Action<ServiceDiscoveryOptions> configureOptions = (_) => { };

        var action = () => services.AddServiceDiscovery(configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(services), exception.ParamName);
    }

    [Fact]
    public void AddServiceDiscoveryWithConfigureOptionsShouldThrowWhenConfigureOptionsIsNull()
    {
        var services = new ServiceCollection();
        Action<ServiceDiscoveryOptions> configureOptions = null!;

        var action = () => services.AddServiceDiscovery(configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(configureOptions), exception.ParamName);
    }

    [Fact]
    public void AddServiceDiscoveryCoreShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        var action = () => services.AddServiceDiscoveryCore();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(services), exception.ParamName);
    }

    [Fact]
    public void AddServiceDiscoveryCoreWithConfigureOptionsShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;
        Action<ServiceDiscoveryOptions> configureOptions = (_) => { };

        var action = () => services.AddServiceDiscoveryCore(configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(services), exception.ParamName);
    }

    [Fact]
    public void AddServiceDiscoveryCoreWithConfigureOptionsShouldThrowWhenConfigureOptionsIsNull()
    {
        var services = new ServiceCollection();
        Action<ServiceDiscoveryOptions> configureOptions = null!;

        var action = () => services.AddServiceDiscoveryCore(configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(configureOptions), exception.ParamName);
    }

    [Fact]
    public void AddConfigurationServiceEndpointProviderShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        var action = () => services.AddConfigurationServiceEndpointProvider();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(services), exception.ParamName);
    }

    [Fact]
    public void AddConfigurationServiceEndpointProviderWithConfigureOptionsShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;
        Action<ConfigurationServiceEndpointProviderOptions> configureOptions = (_) => { };

        var action = () => services.AddConfigurationServiceEndpointProvider(configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(services), exception.ParamName);
    }

    [Fact]
    public void AddConfigurationServiceEndpointProviderWithConfigureOptionsShouldThrowWhenConfigureOptionsIsNull()
    {
        var services = new ServiceCollection();
        Action<ConfigurationServiceEndpointProviderOptions> configureOptions = null!;

        var action = () => services.AddConfigurationServiceEndpointProvider(configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(configureOptions), exception.ParamName);
    }

    [Fact]
    public void AddPassThroughServiceEndpointProviderShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        var action = () => services.AddPassThroughServiceEndpointProvider();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(services), exception.ParamName);
    }

    [Fact]
    public async Task GetEndpointsAsyncShouldThrowWhenServiceNameIsNull()
    {
        var serviceEndpointWatcherFactory = new ServiceEndpointWatcherFactory(
            new List<IServiceEndpointProviderFactory>(),
            new Logger<ServiceEndpointWatcher>(new NullLoggerFactory()),
            Options.Options.Create(new ServiceDiscoveryOptions()),
            TimeProvider.System);

        var serviceEndpointResolver = new ServiceEndpointResolver(serviceEndpointWatcherFactory, TimeProvider.System);
        string serviceName = null!;

        var action = async () => await serviceEndpointResolver.GetEndpointsAsync(serviceName, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.Equal(nameof(serviceName), exception.ParamName);
    }

    [Fact]
    public void CreateShouldThrowWhenEndPointIsNull()
    {
        EndPoint endPoint = null!;

        var action = () => ServiceEndpoint.Create(endPoint);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(endPoint), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryParseShouldThrowWhenEndPointIsNullOrEmpty(bool isNull)
    {
        var input = isNull ? null! : string.Empty;

        var action = () =>
        {
            _ = ServiceEndpointQuery.TryParse(input, out _);
        };

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(input), exception.ParamName);
    }

    [Fact]
    public void CtorServiceEndpointSourceShouldThrowWhenChangeTokenIsNull()
    {
        IChangeToken changeToken = null!;
        var features = new FeatureCollection();
        List<ServiceEndpoint>? endpoints = null;

        var action = () => new ServiceEndpointSource(endpoints, changeToken, features);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(changeToken), exception.ParamName);
    }

    [Fact]
    public void CtorServiceEndpointSourceShouldThrowWhenFeaturesIsNull()
    {
        var changeToken = NullChangeToken.Singleton;
        IFeatureCollection features = null!;
        List<ServiceEndpoint>? endpoints = null;

        var action = () => new ServiceEndpointSource(endpoints, changeToken, features);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(features), exception.ParamName);
    }
}

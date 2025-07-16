// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery;
using Microsoft.Extensions.ServiceDiscovery.Http;

#if NET
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
#endif

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for configuring <see cref="IHttpClientBuilder"/> with service discovery.
/// </summary>
public static class ServiceDiscoveryHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds service discovery to the <see cref="IHttpClientBuilder"/>.
    /// </summary>
    /// <param name="httpClientBuilder">The builder.</param>
    /// <returns>The builder.</returns>
    public static IHttpClientBuilder AddServiceDiscovery(this IHttpClientBuilder httpClientBuilder)
    {
        ArgumentNullException.ThrowIfNull(httpClientBuilder);

        var services = httpClientBuilder.Services;
        services.AddServiceDiscoveryCore();
        httpClientBuilder.AddHttpMessageHandler(services =>
        {
            var timeProvider = services.GetService<TimeProvider>() ?? TimeProvider.System;
            var watcherFactory = services.GetRequiredService<ServiceEndpointWatcherFactory>();
            var registry = new HttpServiceEndpointResolver(watcherFactory, services, timeProvider);
            var options = services.GetRequiredService<IOptions<ServiceDiscoveryOptions>>();
            return new ResolvingHttpDelegatingHandler(registry, options);
        });

#if NET
        // Configure the HttpClient to disable gRPC load balancing.
        // This is done on all HttpClient instances but only impacts gRPC clients.
        AddDisableGrpcLoadBalancingFilter(httpClientBuilder.Services, httpClientBuilder.Name);
#endif
        return httpClientBuilder;
    }

#if NET
    private static void AddDisableGrpcLoadBalancingFilter(IServiceCollection services, string? name)
    {
        // A filter is used because it will always run last. This is important because the disable
        // property needs to be added to all SocketsHttpHandler instances, including those specified
        // with ConfigurePrimaryHttpMessageHandler.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, DisableGrpcLoadBalancingFilter>());
        services.Configure<DisableGrpcLoadBalancingFilterOptions>(o => o.ClientNames.Add(name));
    }

    private sealed class DisableGrpcLoadBalancingFilterOptions
    {
        // Names of clients. A null value means it is applied globally to all clients.
        public HashSet<string?> ClientNames { get; } = new HashSet<string?>();
    }

    private sealed class DisableGrpcLoadBalancingFilter : IHttpMessageHandlerBuilderFilter
    {
        private readonly DisableGrpcLoadBalancingFilterOptions _options;
        private readonly bool _global;

        public DisableGrpcLoadBalancingFilter(IOptions<DisableGrpcLoadBalancingFilterOptions> options)
        {
            _options = options.Value;
            _global = _options.ClientNames.Contains(null);
        }

        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        {
            return (builder) =>
            {
                // Run other configuration first, we want to decorate.
                next(builder);
                if (_global || _options.ClientNames.Contains(builder.Name))
                {
                    if (builder.PrimaryHandler is SocketsHttpHandler socketsHttpHandler)
                    {
                        // gRPC knows about this property and uses it to check whether
                        // load balancing is disabled when the GrpcChannel is created.
                        // see https://github.com/grpc/grpc-dotnet/blob/1625f8942791c82d700802fc7278c543025f0fd3/src/Grpc.Net.Client/GrpcChannel.cs#L286
                        socketsHttpHandler.Properties["__GrpcLoadBalancingDisabled"] = true;
                    }
                }
            };
        }
    }
#endif
}

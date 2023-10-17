// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;
using Microsoft.Extensions.ServiceDiscovery.Internal;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Creates <see cref="ServiceEndPointResolver"/> instances.
/// </summary>
public class ServiceEndPointResolverFactory(
    IEnumerable<IServiceEndPointResolverProvider> resolvers,
    ILogger<ServiceEndPointResolver> resolverLogger,
    IOptions<ServiceEndPointResolverOptions> options,
    TimeProvider timeProvider)
{
    private readonly IServiceEndPointResolverProvider[] _resolverProviders = resolvers
        .Where(r => r is not PassThroughServiceEndPointResolverProvider)
        .Concat(resolvers.Where(static r => r is PassThroughServiceEndPointResolverProvider)).ToArray();
    private readonly ILogger<ServiceEndPointResolver> _resolverLogger = resolverLogger;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IOptions<ServiceEndPointResolverOptions> _options = options;

    /// <summary>
    /// Creates a <see cref="ServiceEndPointResolver"/> instance for the provided service name.
    /// </summary>
    public ServiceEndPointResolver CreateResolver(string serviceName)
    {
        ArgumentNullException.ThrowIfNull(serviceName);

        List<IServiceEndPointResolver>? resolvers = null;
        foreach (var factory in _resolverProviders)
        {
            if (factory.TryCreateResolver(serviceName, out var resolver))
            {
                resolvers ??= new();
                resolvers.Add(resolver);
            }
        }

        if (resolvers is not { Count: > 0 })
        {
            throw new InvalidOperationException("No resolver which supports the provided service name has been configured.");
        }

        return new ServiceEndPointResolver(
            resolvers: resolvers.ToArray(),
            logger: _resolverLogger,
            serviceName: serviceName,
            timeProvider: _timeProvider,
            options: _options);
    }
}

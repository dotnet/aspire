// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery.PassThrough;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Creates service endpoint watchers.
/// </summary>
internal sealed partial class ServiceEndPointWatcherFactory(
    IEnumerable<IServiceEndPointProviderFactory> resolvers,
    ILogger<ServiceEndPointWatcher> resolverLogger,
    IOptions<ServiceDiscoveryOptions> options,
    TimeProvider timeProvider)
{
    private readonly IServiceEndPointProviderFactory[] _resolverProviders = resolvers
        .Where(r => r is not PassThroughServiceEndPointResolverProvider)
        .Concat(resolvers.Where(static r => r is PassThroughServiceEndPointResolverProvider)).ToArray();
    private readonly ILogger<ServiceEndPointWatcher> _logger = resolverLogger;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IOptions<ServiceDiscoveryOptions> _options = options;

    /// <summary>
    /// Creates a service endpoint resolver for the provided service name.
    /// </summary>
    public ServiceEndPointWatcher CreateWatcher(string serviceName)
    {
        ArgumentNullException.ThrowIfNull(serviceName);

        if (!ServiceEndPointQuery.TryParse(serviceName, out var query))
        {
            throw new ArgumentException("The provided input was not in a valid format. It must be a valid URI.", nameof(serviceName));
        }

        List<IServiceEndPointProvider>? resolvers = null;
        foreach (var factory in _resolverProviders)
        {
            if (factory.TryCreateProvider(query, out var resolver))
            {
                resolvers ??= [];
                resolvers.Add(resolver);
            }
        }

        if (resolvers is not { Count: > 0 })
        {
            throw new InvalidOperationException($"No resolver which supports the provided service name, '{serviceName}', has been configured.");
        }

        Log.CreatingResolver(_logger, serviceName, resolvers);
        return new ServiceEndPointWatcher(
            resolvers: [.. resolvers],
            logger: _logger,
            serviceName: serviceName,
            timeProvider: _timeProvider,
            options: _options);
    }
}

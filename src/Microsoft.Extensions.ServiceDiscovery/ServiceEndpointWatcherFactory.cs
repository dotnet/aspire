// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery.PassThrough;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Creates service endpoint watchers.
/// </summary>
internal sealed partial class ServiceEndpointWatcherFactory(
    IEnumerable<IServiceEndpointProviderFactory> providerFactories,
    ILogger<ServiceEndpointWatcher> logger,
    IOptions<ServiceDiscoveryOptions> options,
    TimeProvider timeProvider)
{
    private readonly IServiceEndpointProviderFactory[] _providerFactories = providerFactories
        .Where(r => r is not PassThroughServiceEndpointProviderFactory)
        .Concat(providerFactories.Where(static r => r is PassThroughServiceEndpointProviderFactory)).ToArray();
    private readonly ILogger<ServiceEndpointWatcher> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IOptions<ServiceDiscoveryOptions> _options = options;

    /// <summary>
    /// Creates a service endpoint watcher for the provided service name.
    /// </summary>
    public ServiceEndpointWatcher CreateWatcher(string serviceName)
    {
        ArgumentNullException.ThrowIfNull(serviceName);

        if (!ServiceEndpointQuery.TryParse(serviceName, out var query))
        {
            throw new ArgumentException("The provided input was not in a valid format. It must be a valid URI.", nameof(serviceName));
        }

        List<IServiceEndpointProvider>? providers = null;
        foreach (var factory in _providerFactories)
        {
            if (factory.TryCreateProvider(query, out var provider))
            {
                providers ??= [];
                providers.Add(provider);
            }
        }

        if (providers is not { Count: > 0 })
        {
            throw new InvalidOperationException($"No provider which supports the provided service name, '{serviceName}', has been configured.");
        }

        Log.CreatingResolver(_logger, serviceName, providers);
        return new ServiceEndpointWatcher(
            providers: [.. providers],
            logger: _logger,
            serviceName: serviceName,
            timeProvider: _timeProvider,
            options: _options);
    }
}

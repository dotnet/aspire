// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.ServiceDiscovery;

namespace Microsoft.Extensions.ServiceDiscovery.Yarp;

/// <summary>
/// Implementation of <see cref="IDestinationResolver"/> which resolves destinations using service discovery.
/// </summary>
/// <remarks>
/// Initializes a new <see cref="ServiceDiscoveryDestinationResolver"/> instance.
/// </remarks>
/// <param name="resolver">The endpoint resolver registry.</param>
internal sealed class ServiceDiscoveryDestinationResolver(ServiceEndpointResolver resolver) : IDestinationResolver
{
    /// <inheritdoc/>
    public async ValueTask<ResolvedDestinationCollection> ResolveDestinationsAsync(IReadOnlyDictionary<string, DestinationConfig> destinations, CancellationToken cancellationToken)
    {
        Dictionary<string, DestinationConfig> results = new();
        var tasks = new List<Task<(List<(string Name, DestinationConfig Config)>, IChangeToken ChangeToken)>>(destinations.Count);
        foreach (var (destinationId, destinationConfig) in destinations)
        {
            tasks.Add(ResolveHostAsync(destinationId, destinationConfig, cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        var changeTokens = new List<IChangeToken>();
        foreach (var task in tasks)
        {
            var (configs, changeToken) = await task.ConfigureAwait(false);
            if (changeToken is not null)
            {
                changeTokens.Add(changeToken);
            }

            foreach (var (name, config) in configs)
            {
                results[name] = config;
            }
        }

        return new ResolvedDestinationCollection(results, new CompositeChangeToken(changeTokens));
    }

    private async Task<(List<(string Name, DestinationConfig Config)>, IChangeToken ChangeToken)> ResolveHostAsync(
        string originalName,
        DestinationConfig originalConfig,
        CancellationToken cancellationToken)
    {
        var originalUri = new Uri(originalConfig.Address);
        var originalHost = originalConfig.Host is { Length: > 0 } h ? h : originalUri.Authority;
        var serviceName = originalUri.GetLeftPart(UriPartial.Authority);

        var result = await resolver.GetEndpointsAsync(serviceName, cancellationToken).ConfigureAwait(false);
        var results = new List<(string Name, DestinationConfig Config)>(result.Endpoints.Count);
        var uriBuilder = new UriBuilder(originalUri);
        var healthUri = originalConfig.Health is { Length: > 0 } health ? new Uri(health) : null;
        var healthUriBuilder = healthUri is { } ? new UriBuilder(healthUri) : null;
        foreach (var endpoint in result.Endpoints)
        {
            var addressString = endpoint.ToString()!;
            Uri uri;
            if (!addressString.Contains("://"))
            {
                uri = new Uri($"https://{addressString}");
            }
            else
            {
                uri = new Uri(addressString);
            }

            uriBuilder.Host = uri.Host;
            uriBuilder.Port = uri.Port;
            var resolvedAddress = uriBuilder.Uri.ToString();
            var healthAddress = originalConfig.Health;
            if (healthUriBuilder is not null)
            {
                healthUriBuilder.Host = uri.Host;
                healthUriBuilder.Port = uri.Port;
                healthAddress = healthUriBuilder.Uri.ToString();
            }

            var name = $"{originalName}[{addressString}]";
            var config = originalConfig with { Host = originalHost, Address = resolvedAddress, Health = healthAddress };
            results.Add((name, config));
        }

        return (results, result.ChangeToken);
    }
}
